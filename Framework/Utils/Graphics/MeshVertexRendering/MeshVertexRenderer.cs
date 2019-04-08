﻿using UnityEngine;

namespace Framework
{
	namespace Utils
	{
		public class MeshVertexRenderer : MonoBehaviour
		{
			#region Public Data
			public Renderer _sourceMesh;
			public Shader _vertexBakingShader;
			public Shader _vertexBakingReplacementShader;
			public LayerProperty _vertexBakingLayer;
			public MaterialRefProperty[] _targetMaterials;
			public bool _updateMesh;
			#endregion

			#region Private Data
			private static int[] kAllowedTextureSizes = { 64, 128, 256, 512, 1024, 2048, 4098 };
			private RenderTexture _vertexPositionBuffer;
			private Camera _vertexBufferCamera;
			#endregion

			#region Monobehaviour
			private void Awake()
			{
				CreateVertexBuffer();
				CreateCamera();			
				SetupSkinnedMesh();
				SetupMaterials();
			}

			private void LateUpdate()
			{
#if UNITY_EDITOR
				SetupMaterials();
#endif

				//Render skinned mesh to texture
				_vertexBufferCamera.targetTexture = _vertexPositionBuffer;
				_vertexBufferCamera.RenderWithShader(_vertexBakingShader, "VertexBaking");
			}

			void OnDestroy()
			{
				if (_vertexBufferCamera != null) Destroy(_vertexBufferCamera.gameObject);
				if (_vertexPositionBuffer != null) Destroy(_vertexPositionBuffer);
			}
			#endregion

			#region Puclic Interface
			public static int GetTextureSize(int numVerts)
			{
				int width = 0;

				for (int i = 0; i < kAllowedTextureSizes.Length; i++)
				{
					if (kAllowedTextureSizes[i] * kAllowedTextureSizes[i] > numVerts)
					{
						width = kAllowedTextureSizes[i];
						break;
					}
				}

				return width;
			}
			#endregion

			#region Private Functions
			private int GetVertexCount()
			{
				if (_sourceMesh is SkinnedMeshRenderer)
				{
					return ((SkinnedMeshRenderer)_sourceMesh).sharedMesh.vertexCount;
				}
				else if (_sourceMesh is MeshRenderer)
				{
					MeshFilter meshFilter = _sourceMesh.GetComponent<MeshFilter>();
					return meshFilter.sharedMesh.vertexCount;
				}

				return 0;
			}

			private void CreateVertexBuffer()
			{
				int avatarVertCount = GetVertexCount();

				//Work out min square texture to contain the verts
				int textureSize = GetTextureSize(avatarVertCount);
				_vertexPositionBuffer = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
			}
			
			private void SetupMaterials()
			{
				for (int i=0; i<_targetMaterials.Length; i++)
				{
					_targetMaterials[i].GetMaterial().SetTexture("_VertexPositions", _vertexPositionBuffer);
					_targetMaterials[i].GetMaterial().SetFloat("_VertexPositionsSize", _vertexPositionBuffer.width);
				}
			}

			private void CreateCamera()
			{
				GameObject cameraObj = new GameObject("Avatar Vertex Camera");
				cameraObj.transform.parent = this.transform;
				cameraObj.hideFlags = HideFlags.HideInHierarchy;

				cameraObj.transform.localPosition = Vector3.zero;
				cameraObj.transform.localRotation = Quaternion.identity;

				_vertexBufferCamera = cameraObj.AddComponent<Camera>();
				
				_vertexBufferCamera.clearFlags = CameraClearFlags.SolidColor;
				_vertexBufferCamera.renderingPath = RenderingPath.Forward;
				_vertexBufferCamera.backgroundColor = Color.clear;
				_vertexBufferCamera.depth = -10000;
				_vertexBufferCamera.cullingMask = _vertexBakingLayer.GetLayerMask();

				_vertexBufferCamera.nearClipPlane = -100;
				_vertexBufferCamera.farClipPlane = 100;
				_vertexBufferCamera.orthographic = true;
				_vertexBufferCamera.orthographicSize = 100;

				_vertexBufferCamera.targetTexture = _vertexPositionBuffer;
				_vertexBufferCamera.enabled = false; 
			}

			private void SetupSkinnedMesh()
			{
				_sourceMesh.material = new Material(_vertexBakingReplacementShader);
				_sourceMesh.gameObject.layer = _vertexBakingLayer;	
			}
			#endregion
		}
	}
}
