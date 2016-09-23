using System;
using Urho;
using Urho.Actions;

namespace MyApartments.Desktop
{
	public class SpatialCursor : Component
	{
		Camera camera;
		Octree octree;

		public SpatialCursor(IntPtr handle) : base(handle)
		{
			ReceiveSceneUpdates = true;
		}

		public SpatialCursor()
		{
			ReceiveSceneUpdates = true;
		}

		public Node CursorNode { get; private set; }
		public Node CursorModelNode { get; private set; }
		public bool CursorEnabled { get; set; } = true;

		public event Action<RayQueryResult?> Raycasted;

		public override void OnAttachedToNode(Node node)
		{
			CursorNode = node.CreateChild("SpatialCursor");
			CursorModelNode = CursorNode.CreateChild("SpatialCursorModel");
			CursorModelNode.SetScale(0.05f);
			var staticModel = CursorModelNode.CreateComponent<StaticModel>();
			staticModel.Model = CoreAssets.Models.Torus;
			Material mat = new Material();
			mat.SetTechnique(0, CoreAssets.Techniques.NoTextureOverlay, 1, 1);
			mat.SetShaderParameter("MatDiffColor", Color.Cyan);
			CursorModelNode.RunActions(new RepeatForever(new ScaleTo(0.3f, 0.06f), new ScaleTo(0.3f, 0.04f)));
			staticModel.SetMaterial(mat);
			staticModel.ViewMask = 0x80000000; //hide from raycasts

			base.OnAttachedToNode(node);
			ReceiveSceneUpdates = true;

			// find Octree and Camera components:

			octree = Scene.GetComponent<Octree>(true);
			//camera = Scene.GetComponent<Camera>(true); -- doesn't work! :(( ugly workaround:
			camera = Scene.GetChildrenWithComponent<Camera>(true)[0].GetComponent<Camera>();
		}

		private string lastSurfName = "";

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
			Ray cameraRay = camera.GetScreenRay(0.5f, 0.5f);
			var result = octree.RaycastSingle(cameraRay, RayQueryLevel.Triangle, 100, DrawableFlags.Geometry, 0x70000000);

			var raycastSingle = result != null && result.Count > 0 ? result[0] : (RayQueryResult?)null;

			Raycasted?.Invoke(raycastSingle);
			if (!CursorEnabled)
				return;

			if (raycastSingle != null)
			{
				if (lastSurfName != raycastSingle.Value.Node.Name)
				{
					lastSurfName = raycastSingle.Value.Node.Name;
				}
				CursorNode.Position = raycastSingle.Value.Position;
				CursorNode.Rotation = FromLookRotation(new Vector3(0, 1, 0), raycastSingle.Value.Normal);
			}
			else
				CursorNode.Position = camera.Node.Rotation * new Vector3(0, 0, 5f);
		}

		static Quaternion FromLookRotation(Vector3 direction, Vector3 upDirection)
		{
			Vector3 v = Vector3.Cross(direction, upDirection);
			if (v.LengthSquared >= 0.1f)
			{
				v.Normalize();
				Vector3 y = Vector3.Cross(v, direction);
				Vector3 x = Vector3.Cross(y, direction);
				Matrix3 m3 = new Matrix3(
					x.X, y.X, direction.X,
					x.Y, y.Y, direction.Y,
					x.Z, y.Z, direction.Z);
				return new Quaternion(ref m3);
			}
			return Quaternion.Identity;
		}
	}
}
