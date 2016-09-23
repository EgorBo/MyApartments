using System;

namespace MyApartments
{
	//DTO classes to deserialize data from dump-files via JSON.NET lib
	public struct Vector3Dto
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
	}

	public struct Vector4Dto
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
		public float W { get; set; }
	}

	public class SurfaceDto
	{
		public Guid Id { get; set; }
		public float[] VertexData { get; set; }
		public short[] IndexData { get; set; }

		public Vector3Dto BoundsCenter { get; set; }
		public Vector4Dto BoundsOrientation { get; set; }
	}
}
