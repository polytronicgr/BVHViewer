using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;

namespace BVHViewer
{
	public partial class BVHViewer : Form
	{
		//----------------------------------------------------------------------------------------
		// 定義
		//----------------------------------------------------------------------------------------
		class Node
		{
			public string name;
			public string parentName;
			public Node parent;
			public Vector3 localPos;	// 相対座標

			public Vector3 globalPos;	// 計算後に格納するグローバル座標
		};
		enum Mode
		{
			UNKNOWN,
			BVH,
			OBJ,
		};

		// ビューワ
		const float SCALE = 0.1f;	// BVHスケール
		float _fps = 30f;	// 設定FPS
		int _frame = 0;	// 表示フレーム
		int _modelHandle = -1;

		// ステート
		bool _isPlay = false;
		bool _mouseEnter = false;
		Mode _mode = Mode.UNKNOWN;

		Viewer _viewer;
		BVHParser _bvh;
		List<Node> _nodeList;

		public BVHViewer()
		{
			InitializeComponent();
			_viewer = new Viewer(glControl1);
			_nodeList = new List<Node>();
		}

		/** --------------------------------------------
		 * @brief  BVH構造を取得
		 * @return void
		 * --------------------------------------------*/
		private void setBVH()
		{
			if (_bvh == null) return;
			if (_bvh.IsEnable == false) return;

			// 仮格納
			foreach (var node in _bvh.GetNodeList())
			{
				Node n = new Node();

				n.name = node.Name;
				n.parentName = (node.Parent != null) ? node.Parent.Name : "";
				n.localPos = new Vector3(node.Offset.X, node.Offset.Y, node.Offset.Z) * SCALE;

				_nodeList.Add(n);
			}
			// 親インスタンスを探索、格納
			foreach (var node in _nodeList)
			{
				foreach (var n in _nodeList)
				{
					if (node.parentName == n.name)
					{
						node.parent = n;
						break;
					}
				}

			}

		}

		/** --------------------------------------------
		 * @brief  BVH描画
		 * @param  frame	フレーム
		 * @return void
		 * --------------------------------------------*/
		private void drawBVH(int frame)
		{
			if (_bvh == null) return;
			if (_bvh.IsEnable == false) return;

			// ノードのグローバル座標取得
			foreach (var node in _nodeList)
			{
				Matrix4 matrix = Matrix4.Identity;

				// 最終的な座標変換行列の取得
				Node current = node;
				while (true)
				{
					var bvhNode = _bvh.GetNode(current.name);

					// 相対値
					matrix = matrix *
						GetRotMatrixFromBVHNode(bvhNode, frame) *
						Matrix4.CreateTranslation(current.localPos) *
						Matrix4.Identity;

					// 親に参照を移動
					if (current.parent == null) break;
					current = current.parent;
				}
				node.globalPos = Vector3.Transform(new Vector3(0, 0, 0), matrix);

				// 軸表示
				if (cb_joint_axis.Checked)
				{
					if (node.parent != null)
					{
						_viewer.addLine(node.globalPos, Vector3.Transform(new Vector3(SCALE, 0, 0), matrix), new Vector3(1, 0, 0));
						_viewer.addLine(node.globalPos, Vector3.Transform(new Vector3(0, SCALE, 0), matrix), new Vector3(0, 1, 0));
						_viewer.addLine(node.globalPos, Vector3.Transform(new Vector3(0, 0, SCALE), matrix), new Vector3(0, 0, 1));
					}
				}
			}

			// 描画
			foreach (var node in _nodeList)
			{
				// ジョイント位置
				if (cb_joint_pos.Checked)
				{
					_viewer.addLine(node.globalPos - new Vector3(SCALE, 0, 0), node.globalPos + new Vector3(SCALE, 0, 0), new Vector3(1, 1, 1));
					_viewer.addLine(node.globalPos - new Vector3(0, SCALE, 0), node.globalPos + new Vector3(0, SCALE, 0), new Vector3(1, 1, 1));
					_viewer.addLine(node.globalPos - new Vector3(0, 0, SCALE), node.globalPos + new Vector3(0, 0, SCALE), new Vector3(1, 1, 1));
				}
				// 親子線
				if (cb_parent_line.Checked)
				{
					if (node.parent != null)
					{
						_viewer.addLine(node.globalPos, node.parent.globalPos, new Vector3(1, 1, 0));
					}
				}
			}
		}

		/** --------------------------------------------
		 * @brief  OBJ描画
		 * @return void
		 * --------------------------------------------*/
		private void drawOBJ()
		{
			if (_modelHandle == -1) return;

			_viewer.addModel(_modelHandle, Vector3.Zero, Vector3.Zero, new Vector3(1, 1, 1));
		}

		/** --------------------------------------------
		 * @brief  BVH側のノードから指定フレームの回転行列を返却
		 * @param  node
		 * @param  frame
		 * @return Matrix4
		 * --------------------------------------------*/
		private Matrix4 GetRotMatrixFromBVHNode(BVHParser.Node node, int frame)
		{
			Matrix4 matrix = Matrix4.Identity;

			float deg2rad = (float)(Math.PI / 180.0);

			var euler = node.GetMotionRot(frame);
			for (int i = node.Channnels.Count - 1; i >= 0; i--)
			{
				var channel = node.Channnels[i];
				switch (channel)
				{
					case BVHParser.Node.Channel.Xrotation:
						matrix = matrix * Matrix4.CreateRotationX(euler.X * deg2rad);
						break;
					case BVHParser.Node.Channel.Yrotation:
						matrix = matrix * Matrix4.CreateRotationY(euler.Y * deg2rad);
						break;
					case BVHParser.Node.Channel.Zrotation:
						matrix = matrix * Matrix4.CreateRotationZ(euler.Z * deg2rad);
						break;
					default:
						break;
				}
			}
			return matrix;
		}

		/** --------------------------------------------
		 * @brief  更新
		 * @return void
		 * --------------------------------------------*/
		private void update()
		{
			switch (_mode)
			{
				//!< BVH
				case Mode.BVH:
					{
						// BVHロード済みで再生中なら
						if (_bvh != null && _isPlay)
						{
							_frame++;
							if (_frame >= _bvh.FrameNum)
							{
								_frame = 0;
							}
						}
						//Draw
						drawBVH(_frame);
					}
					break;

				//!< OBJ
				case Mode.OBJ:
					{
						_frame = 0;
						drawOBJ();
					}
					break;

				default: break;
			}

			// Viewer更新
			bool isActive = (Form.ActiveForm == this) && (_mouseEnter);
			_viewer.update(isActive);

			// Control
			button2.Text = _isPlay ? "Pause" : "Play";
			label1.Text = "Frame : " + (_frame + 1).ToString() + " / " + ((_bvh != null) ? (_bvh.FrameNum.ToString()) : "0");
			trackBar1.Value = _frame;
		}

		/** --------------------------------------------
		 * @brief  描画ループ開始
		 * @return void
		 * --------------------------------------------*/
		private void start()
		{
			_viewer.initialize();

			double nextFrame = (double)System.Environment.TickCount;

			while (!this.IsDisposed)
			{
				float period = 1000f / _fps; // フレーム周期
				double tickCount = (double)System.Environment.TickCount; // 現在の時刻を取得

				// 次に処理するフレームの時刻まで間がある場合は、処理をスキップする
				if (tickCount < nextFrame)
				{
					// 1ms以上の間があるか？
					if (nextFrame - tickCount > 1)
					{
						// Sleepする
						System.Threading.Thread.Sleep((int)(nextFrame - tickCount));
					}
					Application.DoEvents();	// Windowsメッセージを処理させる
					continue;
				}

				//!< 更新処理
				update();

				// 描画スキップ
				if ((double)System.Environment.TickCount >= nextFrame + period)
				{
					nextFrame += period;
					continue;
				}

				//!< 描画
				{
					_viewer.render();
				}
				// 次のフレームの時刻を計算する
				nextFrame += period;
			}
		}

		/** --------------------------------------------
		 * @brief  ロード
		 * @param  filename
		 * @return void
		 * --------------------------------------------*/
		private void load(string filename)
		{
			var ext = System.IO.Path.GetExtension(filename).ToLower();
			if (ext != null)
			{
				if (ext.Length > 0)
				{
					switch (ext)
					{
						case ".bvh": loadBVH(filename); break;
						case ".obj": loadOBJ(filename); break;
					}
				}
			}
		}

		/** --------------------------------------------
		 * @brief  BVHロード
		 * @param  filename
		 * @return void
		 * --------------------------------------------*/
		private void loadBVH(string filename)
		{
			_mode = Mode.BVH;

			_isPlay = false;
			_nodeList.Clear();

			_bvh = new BVHParser();
			_bvh.Load(filename);
			setBVH();

			// フレームレート変更
			_fps = 1f / _bvh.FrameSpan;
			label_fps.Text = "FPS : " + _fps.ToString("0.0");
			_frame = 0;

			// control
			trackBar1.Minimum = 0;
			trackBar1.Maximum = _bvh.FrameNum - 1;
		}

		/** --------------------------------------------
		 * @brief  OBJロード
		 * @param  filename
		 * @return void
		 * --------------------------------------------*/
		private void loadOBJ(string filename)
		{
			_mode = Mode.OBJ;

			_modelHandle = _viewer.loadOBJ(filename);
		}

		//----------------------------------------------------------------------------------------
		// コントロールイベント
		//----------------------------------------------------------------------------------------

		//!< @brief 再生ポーズボタン
		private void button2_Click(object sender, EventArgs e) { _isPlay = !_isPlay; }

		//!< @brief トラックバー
		private void trackBar1_Scroll(object sender, EventArgs e)
		{
			_isPlay = false;
			_frame = trackBar1.Value;
		}

		//!< @brief glControlマウス
		private void glControl1_MouseEnter(object sender, EventArgs e) { _mouseEnter = true; }

		private void glControl1_MouseLeave(object sender, EventArgs e) { _mouseEnter = false; }

		//!< @brief  フォーム表示時イベント
		private void Form1_Shown(object sender, EventArgs e) { start(); }

		//!< @brief Load Button
		private void button1_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.Filter = "MotionFile|*.bvh|WavefrontObj|*.obj";
			if (ofd.ShowDialog() == DialogResult.OK)
			{
				load(ofd.FileName);
			}
		}

		//!< @brief D&D
		private void dragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.Copy;
			else
				e.Effect = DragDropEffects.None;
		}
		private void dragDrop(object sender, DragEventArgs e)
		{
			string[] fileName = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			load(fileName[0]);
		}

		//!< @brief D&D
		private void glControl1_DragDrop(object sender, DragEventArgs e)
		{
			string[] fileName = (string[])e.Data.GetData(DataFormats.FileDrop, false);
			load(fileName[0]);
		}

		private void glControl1_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effect = DragDropEffects.Copy;
			else
				e.Effect = DragDropEffects.None;
		}

	}
}
