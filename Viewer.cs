using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BVHViewer
{
	class Viewer
	{
		//----------------------------------------------------------------------------------------
		// 定義 
		//----------------------------------------------------------------------------------------
		struct Vertex
		{
			Vector2 uv;
			Color4	color;
			Vector3	normal;
			Vector3 pos;
			public Vertex(Vector3 _pos, Vector3 _normal, Vector2 _uv, Color4 _color)
			{
				pos		= _pos;
				normal	= _normal;
				color	= _color;
				uv		= _uv;
			}
			public static readonly int Stride = Marshal.SizeOf(default(Vertex));
		};


		// glcontrol
		GLControl _glControl;

		// リソースハンドル
		private List<Tuple<int, int, int>> _models_res; // vertex, index, index_num

		// 描画キュー
		private List<Tuple<Vector3, Vector3, Vector4>>		_lines;
		private List<Tuple<Vector3, Vector4>>				_points;
		private List<Tuple<int, Vector3, Vector3, Vector3>>	_models; // handle, pos, rotation, scale

		// カメラパラメータ
		private Vector3 _cameraPos = new Vector3(0, 0, 4);
		private Vector3 _cameraUp = new Vector3(0, 1, 0);

		private Vector3 _rotYawPitch = new Vector3(-3.14f * 40.0f / 180.0f, 0, 0);
		private Vector3 _offsetPos = new Vector3(0, 0, 0);

		// スケーリング
		private Vector3 _scale = new Vector3(1, 1, 1);

		// 回転行列保持
		private Matrix4 _rotmatrix = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

		// マウスステート
		int _oldMouseX = 0;
		int _oldMouseY = 0;
		int _oldMouseZ = 0;

		public Viewer(GLControl glcontrol)
		{
			_glControl = glcontrol;

			// りそーすハンドル
			_models_res = new List<Tuple<int, int, int>>();

			// キュー生成
			_lines = new List<Tuple<Vector3, Vector3, Vector4>>();
			_points = new List<Tuple<Vector3, Vector4>>();
			_models = new List<Tuple<int, Vector3, Vector3, Vector3>>();
		}

		/** --------------------------------------------
		 * @brief  初期化
		 * @return void
		 * --------------------------------------------*/
		public void initialize()
		{
			_glControl.MakeCurrent();

			GL.ClearColor(Color4.Black);
			GL.Enable(EnableCap.DepthTest);

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			GL.PointSize(3.0f);
			GL.LineWidth(1.0f);

			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.NormalArray);
			GL.EnableClientState(ArrayCap.ColorArray);
			GL.EnableClientState(ArrayCap.TextureCoordArray);
			GL.EnableClientState(ArrayCap.IndexArray);
		}

		public void release()
		{
			foreach(var model in _models_res)
			{
				GL.DeleteBuffer(model.Item1);
				GL.DeleteBuffer(model.Item2);
			}
			GL.DisableClientState(ArrayCap.VertexArray);
			GL.DisableClientState(ArrayCap.NormalArray);
			GL.DisableClientState(ArrayCap.ColorArray);
		}

		/** --------------------------------------------
		 * @brief  更新処理
		 * @param  cameraUpdate	カメラの更新許可
		 * @return void
		 * --------------------------------------------*/
		public void update(bool cameraUpdate)
		{
			// 視点回転用マトリクス( 更新内容は次フレームから反映させる )
			_rotmatrix = Matrix4.CreateRotationX(_rotYawPitch.X) * Matrix4.CreateRotationY(_rotYawPitch.Y);

			//!< インプット取得
			var keyboard = OpenTK.Input.Keyboard.GetState();
			var mouse	= OpenTK.Input.Mouse.GetCursorState();

			// 差分取得
			int diffX = mouse.X - _oldMouseX;
			int diffY = mouse.Y - _oldMouseY;
			int diffZ = mouse.Wheel - _oldMouseZ;

			// 入力取得
			bool isPushLeft		= mouse.LeftButton == OpenTK.Input.ButtonState.Pressed;
			bool isPushRight	= mouse.RightButton == OpenTK.Input.ButtonState.Pressed;
			bool isPushMiddle	= mouse.MiddleButton == OpenTK.Input.ButtonState.Pressed;
			bool isPushLCTRL	= keyboard.IsKeyDown(OpenTK.Input.Key.LControl);

			// カメラ更新
			if (cameraUpdate)
			{
				// 視点回転
				if (isPushLeft)
				{
					_rotYawPitch.X -= 0.01f * diffY;
					_rotYawPitch.Y -= 0.01f * diffX;
					drawIdenityVec(_offsetPos, 0.5f); // 回転中心に軸表示
				}
				// 視点移動
				else if (isPushRight)
				{
					_offsetPos += Vector3.Transform(new Vector3((float)-diffX, (float)diffY, 0), _rotmatrix) * 0.01f;
					drawIdenityVec(_offsetPos, 0.5f); // 回転中心に軸表示
				}

				if (isPushLCTRL)
				{
					// スケール
					_scale += new Vector3(0.2f, 0.2f, 0.2f) * diffZ;
				}
				else
				{
					// 視点ローカルZ軸位置
					_cameraPos.Z -= 0.5f * diffZ;
				}
			}
			// 値更新
			_oldMouseX = mouse.X;
			_oldMouseY = mouse.Y;
			_oldMouseZ = mouse.Wheel;

			// グリッド表示
			//drawIdenityVec(new Vector3(0, 0, 0), 1);
			drawGrid(0.5f, 20, new Vector4(0.76f, 0.76f, 0.76f, 0.5f));
		}

		/** --------------------------------------------
		 * @brief  描画キューの描画
		 * @return void
		 * --------------------------------------------*/
		public void render()
		{
			if (_glControl.IsDisposed) return;

			_glControl.MakeCurrent();
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			// ワールド * ビュー
			var camPos = Vector3.Transform(_cameraPos, _rotmatrix) + _offsetPos;
			var camUp = Vector3.Transform(_cameraUp, _rotmatrix);
			Matrix4 modelviewMat = Matrix4.CreateScale(_scale) * Matrix4.LookAt(camPos, _offsetPos, camUp);

			// 射影
			GL.Viewport(0, 0, _glControl.Size.Width, _glControl.Size.Height);
			GL.MatrixMode(MatrixMode.Projection);
			Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, (float)_glControl.Size.Width / (float)_glControl.Size.Height, 1.0f, 64.0f);
			GL.LoadMatrix(ref projection);

			//!< モデルビュー行列設定
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();
			GL.LoadMatrix(ref modelviewMat);

			// LineDraw
			GL.Begin(PrimitiveType.Lines);
			{
				foreach (var line in _lines)
				{
					GL.Color4(line.Item3);
					GL.Vertex3(line.Item1);
					GL.Vertex3(line.Item2);
				}
				_lines.Clear();
			}
			GL.End();

			// PointDraw
			GL.Begin(PrimitiveType.Points);
			{
				foreach (var point in _points)
				{
					GL.Color4(point.Item2);
					GL.Vertex3(point.Item1);
				}
				_points.Clear();
			}
			GL.End();

			// ModelDraw
			foreach(var model in _models)
			{
				// リソース
				var res = _models_res[model.Item1];
				int vbo		= res.Item1;
				int ibo		= res.Item2;
				int idx_num = res.Item3;

				// パラメータ
				var pos		= model.Item2;
				var rot		= model.Item3;
				var scale	= model.Item4;

				// リソースバインド
				GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);

				// 頂点構造設定
				//GL.InterleavedArrays(InterleavedArrayFormat.T2fC4fN3fV3f,0, (IntPtr)null);

				// 描画
				GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
				//GL.DrawElements(PrimitiveType.Triangles, idx_num, DrawElementsType.UnsignedInt, 0);
			}
			_models.Clear();

			//Flip
			_glControl.SwapBuffers();			
		}

		/** --------------------------------------------
		 * @brief  OBJロード
		 * @param  filename
		 * @return int		リソースハンドル
		 * --------------------------------------------*/
		public int loadOBJ(string filename)
		{
			int handle = _models_res.Count;

			int vbo	= -1;
			int ibo	= -1;

			var vertices = new List<Vertex>();
			var indices = new List<uint>();

			vertices.Add(new Vertex(new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector2(0, 0), new Color4(1, 1, 1, 1)));
			vertices.Add(new Vertex(new Vector3(100, 0, 0), new Vector3(0, 1, 0), new Vector2(0, 0), new Color4(1, 1, 1, 1)));
			vertices.Add(new Vertex(new Vector3(100, 100, 0), new Vector3(0, 1, 0), new Vector2(0, 0), new Color4(1, 1, 1, 1)));

			indices.Add(0);
			indices.Add(1);
			indices.Add(2);

			// 頂点バッファ
			GL.GenBuffers(1, out vbo);						// リソースバッファ生成
			GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);	// リソースハンドルをバッファとバインド(紐づけ)
			GL.BufferData<Vertex>(BufferTarget.ArrayBuffer, new IntPtr(vertices.Count * Vertex.Stride), vertices.ToArray(), BufferUsageHint.StaticDraw); // バッファセット
			
			// インデックスバッファ
			GL.GenBuffers(1, out ibo);
			GL.BufferData<uint>(BufferTarget.ElementArrayBuffer, new IntPtr(sizeof(uint)*indices.Count), indices.ToArray(), BufferUsageHint.StaticDraw);

			// 解除
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			// resource handle add
			_models_res.Add(new Tuple<int, int, int>(vbo, ibo, indices.Count));

			return handle;
		}

		//!< @brief 線を追加
		public void addLine(Vector3 from, Vector3 to, Vector4 color) { _lines.Add(new Tuple<Vector3, Vector3, Vector4>(from, to, color)); }
		public void addLine(Vector3 from, Vector3 to, Vector3 color) { addLine(from, to, new Vector4(color.X, color.Y, color.Z, 1)); }
		public void addLine(Vector3 from, Vector3 to, OpenTK.Graphics.Color4 color) { addLine(from, to, new Vector4(color.R, color.G, color.B, color.A)); }

		//!< @brief 点を追加
		public void addPoint(Vector3 pt, Vector4 color) { _points.Add(new Tuple<Vector3, Vector4>(pt, color)); }
		public void addPoint(Vector3 pt, Vector3 color) { addPoint(pt, new Vector4(color.X, color.Y, color.Z, 1)); }
		public void addPoint(Vector3 pt, OpenTK.Graphics.Color4 color) { addPoint(pt, new Vector4(color.R, color.G, color.B, color.A)); }

		//!< @brief モデルを追加
		public void addModel(int handle, Vector3 pos, Vector3 rot, Vector3 scale){_models.Add( new Tuple<int,Vector3,Vector3,Vector3>(handle, pos, rot, scale));}

		/** --------------------------------------------
		 * @brief  単位ベクトル表示
		 * @param  pos		位置
		 * @param  length	長さ
		 * @return void
		 * --------------------------------------------*/
		private void drawIdenityVec(Vector3 pos, float length)
		{
			addLine(pos, pos + new Vector3(length, 0, 0), new Vector4(1, 0, 0, 1));
			addLine(pos, pos + new Vector3(0, length, 0), new Vector4(0, 1, 0, 1));
			addLine(pos, pos + new Vector3(0, 0, length), new Vector4(0, 0, 1, 1));
		}

		/** --------------------------------------------
		 * @brief  Grid用のライン
		 * @param  sideOfGrid			四角1辺の長さ
		 * @param  quadNumOfGridSide	1辺の四角数(偶数)
		 * @param  color				色
		 * @return void
		 * --------------------------------------------*/
		private void drawGrid(float sideOfGrid, int quadNumOfGridSide, Vector4 color)
		{
			//!< XZ平面
			for (int i = 0; i < quadNumOfGridSide / 2; ++i)
			{
				addLine(new Vector3((i * sideOfGrid), 0, (sideOfGrid * quadNumOfGridSide) / 2), new Vector3((i * sideOfGrid), 0, -(sideOfGrid * quadNumOfGridSide) / 2), color);
				addLine(new Vector3(-(i * sideOfGrid), 0, (sideOfGrid * quadNumOfGridSide) / 2), new Vector3(-(i * sideOfGrid), 0, -(sideOfGrid * quadNumOfGridSide) / 2), color);

				addLine(new Vector3((sideOfGrid * quadNumOfGridSide) / 2, 0, (i * sideOfGrid)), new Vector3(-(sideOfGrid * quadNumOfGridSide) / 2, 0, (i * sideOfGrid)), color);
				addLine(new Vector3((sideOfGrid * quadNumOfGridSide) / 2, 0, -(i * sideOfGrid)), new Vector3(-(sideOfGrid * quadNumOfGridSide) / 2, 0, -(i * sideOfGrid)), color);
			}
		}
	}
}
