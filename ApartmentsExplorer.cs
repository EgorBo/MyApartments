using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyApartments.Desktop;
using Newtonsoft.Json;
using Urho;
using Urho.Gui;
using Urho.Actions;
using Urho.Navigation;
using Urho.Shapes;

namespace MyApartments
{
	public class ApartmentsExplorer : Application
	{
		float yaw;
		float pitch;
		Node cameraNode;
		Scene scene;

		public ApartmentsExplorer() : base(new ApplicationOptions("Data") { Width = 1286, Height = 720 })
		{
		}

		protected override void Start()
		{
			scene = new Scene();
			scene.CreateComponent<Octree>();
			var zone = scene.CreateComponent<Zone>();
			zone.AmbientColor = new Color(0.8f, 0.8f, 0.8f);

			// Camera and Viewport
			cameraNode = scene.CreateChild();
			var camera = cameraNode.CreateComponent<Camera>();
			var leftViewport = new Viewport(scene, camera, null);
			Renderer.SetViewport(0, leftViewport);

			// emulate HoloLens:
			camera.Fov = 30;
			camera.NearClip = 0.1f;
			camera.FarClip = 20f;

			// directional light
			Node lightNode = scene.CreateChild();
			lightNode.SetDirection(new Vector3(0, -1, 0));
			Light light = lightNode.CreateComponent<Light>();
			light.LightType = LightType.Directional;

			// just a box in the 0,0,0 position (origin).
			var originPointNode = scene.CreateChild();
			originPointNode.SetScale(0.2f);
			var box = originPointNode.CreateComponent<Box>();
			box.SetMaterial(ResourceCache.GetMaterial("Materials/BoxMaterial.xml"));

			// load spatial data
			var files = Directory.GetFiles(@"Data/Surfaces");
			foreach (var file in files)
			{
				var surface = JsonConvert.DeserializeObject<SurfaceDto>(File.ReadAllText(file));

				var child = scene.CreateChild();
				child.Position = new Vector3(surface.BoundsCenter.X, surface.BoundsCenter.Y, surface.BoundsCenter.Z);
				child.Rotation = new Quaternion(surface.BoundsOrientation.X, surface.BoundsOrientation.Y, surface.BoundsOrientation.Z, surface.BoundsOrientation.W);

				var staticModel = child.CreateComponent<StaticModel>();
				staticModel.Model = CreateModelFromVertexData(surface.VertexData, surface.IndexData);
				var randomMaterial = Material.FromColor(new Color(Randoms.Next(0.3f, 0.6f), Randoms.Next(0.3f, 0.6f), Randoms.Next(0.3f, 0.6f)));
				randomMaterial.FillMode = FillMode.Solid; // change to Wireframe
				staticModel.SetMaterial(randomMaterial);
			}

			cameraNode.Position = new Vector3(2, 0, 0);

			// spatial cursor
			scene.CreateComponent<SpatialCursor>();
		}

		unsafe Model CreateModelFromVertexData(float[] vertexData, short[] indexData)
		{
			// this code is based on
			// https://github.com/xamarin/urho-samples/blob/master/FeatureSamples/Core/34_DynamicGeometry/DynamicGeometry.cs#L188-L189

			// vertexData - is an array of:
			// { PositionX, PositionY, PositionZ, NormalX, NormalY, NormalZ } * VerticesCount
			// according to a mask: "ElementMask.Position | ElementMask.Normal".

			var model = new Model();
			var vertexBuffer = new VertexBuffer(Context, false);
			var indexBuffer = new IndexBuffer(Context, false);
			var geometry = new Geometry();

			vertexBuffer.Shadowed = true;
			vertexBuffer.SetSize((uint)vertexData.Length / 6 /*6 components for a single vertex, see the comment above*/, ElementMask.Position | ElementMask.Normal, false);

			fixed (float* p = &vertexData[0])
			{
				vertexBuffer.SetData((void*)p);
			}

			indexBuffer.Shadowed = true;
			indexBuffer.SetSize((uint)indexData.Length, false, false);
			indexBuffer.SetData(indexData);

			geometry.SetVertexBuffer(0, vertexBuffer);
			geometry.IndexBuffer = indexBuffer;
			geometry.SetDrawRange(PrimitiveType.TriangleList, 0, (uint)indexData.Length, true);

			model.NumGeometries = 1;
			model.SetGeometry(0, 0, geometry);
			model.BoundingBox = new BoundingBox(new Vector3(-1.0f, -1.0f, -1.0f), new Vector3(1.0f, 1.0f, 1.0f));

			return model;
		}

		protected override void OnUpdate(float timeStep)
		{
			// rotate & move camera by mouse and WASD:

			const float mouseSensitivity = .1f;
			const float moveSpeed = 5;

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