using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Urho;

namespace MyApartments
{
	class Program
	{
		static void Main(string[] args)
		{
			new MeshViewerApp("Data") { }.Run();
		}
	}

	public class MeshViewerApp : Application
	{
		Node environmentNode;
		Node lightNode;

		public MeshViewerApp(string o) : base(new ApplicationOptions(o))
		{
		}

		protected override unsafe void Start()
		{
			var scene = new Scene();
			scene.CreateComponent<Octree>();
			var zone = scene.CreateComponent<Zone>();
			zone.AmbientColor = new Color(0.6f, 0.6f, 0.6f);

			cameraNode = scene.CreateChild();
			var camera = cameraNode.CreateComponent<Camera>();

			var viewport = new Viewport(scene, camera, null);
			Renderer.SetViewport(0, viewport);

			lightNode = scene.CreateChild();
			lightNode.Position = new Vector3(0, 3, 0);
			var light = lightNode.CreateComponent<Light>();
			light.LightType = LightType.Directional;
			light.Brightness = 2f;
			light.Range = 200;

			environmentNode = scene.CreateChild();
			environmentNode.SetScale(0.2f);

			var surfs = JsonConvert.DeserializeObject<Dictionary<string, SurfaceDto>>(File.ReadAllText(@"Data\UrhoSpatialMappingData.txt"));
			new MonoDebugHud(this).Show();

			var material = ResourceCache.GetMaterial("Material.xml");
			material.CullMode = CullMode.Ccw;
			material.FillMode = FillMode.Solid;

			foreach (var item in surfs)
			{
				var surface = item.Value;
				var orient = surface.BoundsOrientation;
				var bounds = surface.BoundsCenter;

				var child = environmentNode.CreateChild(item.Key);

				var staticModel = child.CreateComponent<StaticModel>();
				staticModel.Model = CreateModelFromVertexData(surface);
				child.Position = *(Vector3*)(void*)&bounds;
				child.Rotation = *(Quaternion*)(void*)&orient;
				staticModel.SetMaterial(material);
				//staticModel.SetMaterial(Material.FromColor(new Color(Randoms.Next(0.3f, 0.7f), Randoms.Next(0.3f, 0.7f), Randoms.Next(0.3f, 0.7f))));
			}
		}

		unsafe Model CreateModelFromVertexData(SurfaceDto surface)
		{
			var model = new Model();
			var vertexBuffer = new VertexBuffer(Context, false);
			var indexBuffer = new IndexBuffer(Context, false);
			var geometry = new Geometry();

			vertexBuffer.Shadowed = true;
			vertexBuffer.SetSize((uint)surface.VertexData.Length, ElementMask.Position | ElementMask.Normal | ElementMask.Color, false);

			fixed (SpatialVertexDto* p = &surface.VertexData[0])
			{
				vertexBuffer.SetData((void*)p);
			}

			var indexData = surface.IndexData;
			indexBuffer.Shadowed = true;
			indexBuffer.SetSize((uint)indexData.Length, false, false);
			indexBuffer.SetData(indexData);

			geometry.SetVertexBuffer(0, vertexBuffer);
			geometry.IndexBuffer = indexBuffer;
			geometry.SetDrawRange(PrimitiveType.TriangleList, 0, (uint)indexData.Length, 0, (uint)surface.VertexData.Length, true);

			model.NumGeometries = 1;
			model.SetGeometry(0, 0, geometry);
			model.BoundingBox = new BoundingBox(new Vector3(-1.26f, -1.26f, -1.26f), new Vector3(1.26f, 1.26f, 1.26f));

			return model;
		}

		protected override void OnUpdate(float timeStep)
		{
			lightNode.SetDirection(cameraNode.Direction);
			EmulateCamera(timeStep);
			base.OnUpdate(timeStep);
		}

		float yaw;
		float pitch;
		Node cameraNode;

		void EmulateCamera(float timeStep, float moveSpeed = 2.0f)
		{
			const float mouseSensitivity = .1f;

			if (UI.FocusElement != null)
				return;

			var mouseMove = Input.MouseMove;
			yaw += mouseSensitivity * mouseMove.X;
			pitch += mouseSensitivity * mouseMove.Y;
			pitch = MathHelper.Clamp(pitch, -90, 90);

			cameraNode.Rotation = new Quaternion(pitch, yaw, 0);

			if (Input.GetKeyDown(Key.W)) cameraNode.Translate(Vector3.UnitZ * moveSpeed * timeStep);
			if (Input.GetKeyDown(Key.S)) cameraNode.Translate(-Vector3.UnitZ * moveSpeed * timeStep);
			if (Input.GetKeyDown(Key.A)) cameraNode.Translate(-Vector3.UnitX * moveSpeed * timeStep);
			if (Input.GetKeyDown(Key.D)) cameraNode.Translate(Vector3.UnitX * moveSpeed * timeStep);
		}
	}
}
