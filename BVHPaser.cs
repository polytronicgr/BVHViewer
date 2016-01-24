/** --------------------------------------------
 * @file   BVHPaser.cs
 * @brief  BVH読み込みクラス
 * @author ponkotou
 * @date   2015/07/20
 * --------------------------------------------*/

/* テキストベースのフォーマットなので、
 * 生成ソフトによっては若干の方言がある可能性があり、
 * 場合のよっては読み込めない、正しく読み込めない可能性はある。
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace BVHViewer
{
	class BVHParser
	{
		public struct Vec3
		{
			public float X { set; get; }
			public float Y { set; get; }
			public float Z { set; get; }

			public Vec3(float x, float y, float z)
				: this()
			{
				X = x; Y = y; Z = z;
			}
		};

		//----------------------------------------------------------------------------------------
		// 定義
		//----------------------------------------------------------------------------------------
		enum Mode
		{
			UNKNOWN,
			HIERARCHY,
			MOTION,
		};

		//!< ノード
		public class Node
		{
			public enum Channel
			{
				UNKNOWN,
				Xposition, Yposition, Zposition,
				Xrotation, Yrotation, Zrotation,
			};

			public enum eElement
			{
				X,
				Y,
				Z,
			};

			public Node Parent { set; get; }
			public String Name { set; get; }
			public Vec3 Offset { set; get; }
			public List<Channel> Channnels { set; get; }
			public List<Node> Nodes { set; get; }

			private Dictionary<int, Vec3> motionPos;
			private Dictionary<int, Vec3> motionRot;

			//!< @brief モーション,位置追加
			public void SetMotionPos(int frame, Vec3 pos)
			{
				if (motionPos.ContainsKey(frame) == false) motionPos.Add(frame, new Vec3());
				motionPos[frame] = pos;
			}
			public void SetMotionPos(int frame, float pos, eElement e)
			{
				if (motionPos.ContainsKey(frame) == false) motionPos.Add(frame, new Vec3());
				Vec3 newVal = motionPos[frame];
				switch (e)
				{
					case eElement.X: newVal.X = pos; break;
					case eElement.Y: newVal.Y = pos; break;
					case eElement.Z: newVal.Z = pos; break;
				}
				motionPos[frame] = newVal;
			}

			//!< @brief モーション,回転追加
			public void SetMotionRot(int frame, Vec3 rot)
			{
				if (motionRot.ContainsKey(frame) == false) motionRot.Add(frame, new Vec3());
				motionRot[frame] = rot;
			}
			public void SetMotionRot(int frame, float rot, eElement e)
			{
				if (motionRot.ContainsKey(frame) == false) motionRot.Add(frame, new Vec3());
				Vec3 newVal = motionRot[frame];
				switch (e)
				{
					case eElement.X: newVal.X = rot; break;
					case eElement.Y: newVal.Y = rot; break;
					case eElement.Z: newVal.Z = rot; break;
				}
				motionRot[frame] = newVal;
			}
			//!< @brief フレーム時位置取得
			public Vec3 GetMotionPos(int frame)
			{
				return motionPos.ContainsKey(frame) ? motionPos[frame] : new Vec3(0, 0, 0);
			}
			//!< @brief フレーム時回転取得
			public Vec3 GetMotionRot(int frame)
			{
				return motionRot.ContainsKey(frame) ? motionRot[frame] : new Vec3(0, 0, 0);
			}

			public Node()
			{
				Channnels = new List<Channel>();
				Nodes = new List<Node>();
				motionPos = new Dictionary<int, Vec3>();
				motionRot = new Dictionary<int, Vec3>();
			}
		};

		//----------------------------------------------------------------------------------------
		// 宣言
		//----------------------------------------------------------------------------------------		
		Mode _mode;

		Node _root;
		Node _target;

		List<Node> _nodeOrder; // MOTION用のノードの順番記憶
		Dictionary<string, Node> _nodeDic;	// ノード名からノードインスタンスを引っ張る

		//MOTION
		bool isFrameNum = false, isFrameSpan = false;
		int motionNum = 0;

		//----------------------------------------------------------------------------------------
		// アクセサ
		//----------------------------------------------------------------------------------------
		public bool IsEnable { get; private set; }
		public int FrameNum { get; private set; }
		public float FrameSpan { get; private set; }

		/** --------------------------------------------
		 * @brief  ルートノード取得
		 * @return Node
		 * --------------------------------------------*/
		public Node GetRootNode()
		{
			return _root;
		}

		/** --------------------------------------------
		 * @brief  ノード取得
		 * @param  name	ノード名
		 * @return Node
		 * --------------------------------------------*/
		public Node GetNode(string name)
		{
			//まあとりあえず線形探索する・・・。
			// MOITON楊のノード順番のほうに全ノード入っている(はず)なので、そこでやってしまう。

			return _nodeDic.ContainsKey(name) ? _nodeDic[name] : null;
		}

		/** --------------------------------------------
		 * @brief  全ノード一覧を返却
		 * @return List<Node>
		 * --------------------------------------------*/
		public List<Node> GetNodeList()
		{
			return _nodeOrder;

		}

		/** --------------------------------------------
		 * @brief  コンストラクタ
		 * --------------------------------------------*/
		public BVHParser()
		{
			_mode = Mode.UNKNOWN;
			_nodeOrder = new List<Node>();
			_nodeDic = new Dictionary<string, Node>();
			IsEnable = false;
		}

		/** --------------------------------------------
		 * @brief  ロード
		 * @param  filepath	ファイルパス
		 * @return void
		 * --------------------------------------------*/
		public void Load(string filepath)
		{
			// 初期値設定  再ロード可能
			_mode = Mode.UNKNOWN;
			_root = null;
			_target = null;
			isFrameNum = false;
			isFrameSpan = false;
			motionNum = 0;
			_nodeOrder.Clear();
			_nodeDic.Clear();

			// ファイルロード
			using (StreamReader sr = new StreamReader(filepath, System.Text.Encoding.GetEncoding("shift_jis")))
			{
				Load(sr);
			}
		}

		public void Load(StreamReader sr)
		{
			while (sr.Peek() != -1)
			{
				// split
				String[] words = sr.ReadLine().Split(' ');

				// 空白(スペース),タブ文字削除
				for (int i = 0; i < words.Length; i++)
					words[i] = words[i].Trim();

				// 文字なし消去
				List<string> word_list = new List<string>();
				for (int i = 0; i < words.Length; i++)
				{
					if (words[i] != "")
					{
						word_list.Add(words[i]);
					}
				}

				if (word_list.Count == 0) continue;

				if (word_list[0] == "HIERARCHY")
				{
					_mode = Mode.HIERARCHY;
					continue;
				}
				else if (word_list[0] == "MOTION")
				{
					_mode = Mode.MOTION;
					continue;
				}
				switch (_mode)
				{
					// 構造部
					case Mode.HIERARCHY:
						// End Site以下{\n Offset x,y,z\n }\nの3行を読み飛ばす
						if (word_list[0] == "End" && word_list[1] == "Site")
						{
							sr.ReadLine();
							sr.ReadLine();
							sr.ReadLine();
							break;
						}

						if (ParseHierarchy(word_list.ToArray()) == false)
						{
							MessageBox.Show("ParseHierarchy Error");
							return;
						}
						break;

					// モーション部
					case Mode.MOTION:
						if (ParseMotion(word_list.ToArray()) == false)
						{
							MessageBox.Show("ParseMotion Error");
							return;
						}
						break;

					default: break;
				}
			} //end while

			// NodeDictionary生成
			foreach (var node in _nodeOrder)
			{
				_nodeDic.Add(node.Name, node);
			}

			IsEnable = true;
		}

		/** --------------------------------------------
		 * @brief  構造部のパース
		 * @param  words	1行の中のスペース区切りの単語
		 * @return bool
		 * --------------------------------------------*/
		private bool ParseHierarchy(String[] words)
		{
			switch (words[0])
			{
				//Root
				case "ROOT":
					{
						_root = new Node();
						_root.Parent = null;
						_root.Name = words[1];
						_target = _root;

						_nodeOrder.Add(_target);
					}
					break;

				//一応用意。
				case "{":
					{

					}
					break;

				//親子階層を1段戻す
				case "}":
					{
						_target = _target.Parent;
					}
					break;

				// オフセット
				case "OFFSET":
					{
						_target.Offset = new Vec3(float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3]));
					}
					break;

				// ノードに含まれる情報 モーション部との連携部分
				case "CHANNELS":
					{
						int chNum = int.Parse(words[1]);	// CHANNNEL数
						for (int i = 0; i < chNum; i++)
						{
							Node.Channel channel = Node.Channel.UNKNOWN;
							if (words[i + 2] == Node.Channel.Xposition.ToString()) channel = Node.Channel.Xposition;
							else if (words[i + 2] == Node.Channel.Yposition.ToString()) channel = Node.Channel.Yposition;
							else if (words[i + 2] == Node.Channel.Zposition.ToString()) channel = Node.Channel.Zposition;
							else if (words[i + 2] == Node.Channel.Xrotation.ToString()) channel = Node.Channel.Xrotation;
							else if (words[i + 2] == Node.Channel.Yrotation.ToString()) channel = Node.Channel.Yrotation;
							else if (words[i + 2] == Node.Channel.Zrotation.ToString()) channel = Node.Channel.Zrotation;

							if (channel == Node.Channel.UNKNOWN) return false;	// 上記のどれかであるはずなので、例外処理をしておく。
							_target.Channnels.Add(channel);
						}
					}
					break;

				// 子ジョイントを追加し、ターゲットノードに設定
				case "JOINT":
					{
						var child = new Node();
						child.Parent = _target;		// 親は現在のターゲットノード
						child.Name = words[1];		// ジョイント名
						_target.Nodes.Add(child);		// 子ジョイント追加

						_target = child;		// ターゲットノード再設定

						_nodeOrder.Add(_target);		// チャンネル追加順序の記憶
					}
					break;

				default: break;
			}
			return true;
		}

		/** --------------------------------------------
		 * @brief  モーション部のパース
		 * @param  words	1行の中のスペース区切りの単語
		 * @return bool
		 * --------------------------------------------*/
		private bool ParseMotion(String[] words)
		{
			if (!isFrameNum || !isFrameSpan)
			{
				//Frame数
				if (words[0] == "Frames:")
				{
					FrameNum = int.Parse(words[1]);
					isFrameNum = true;
				}
				else if (words[0] == "Frames" && words[1] == ":")
				{
					FrameNum = int.Parse(words[2]);
					isFrameNum = true;
				}
				//Frame間隔
				else if (words[0] == "Frame" && words[1] == "Time:")
				{
					FrameSpan = float.Parse(words[2]);
					isFrameSpan = true;
				}
				else if (words[0] == "Frame" && words[1] == "Time" && words[2] == ":")
				{
					FrameSpan = float.Parse(words[3]);
					isFrameSpan = true;
				}
			}
			else
			{
				//同一フレーム間
				int wordIndex = 0;
				for (int i = 0; i < _nodeOrder.Count; i++)
				{
					var node = _nodeOrder[i];

					Vec3 pos = new Vec3();
					Vec3 rot = new Vec3();
					for (int ch = 0; ch < node.Channnels.Count; ch++)
					{

						if (words[wordIndex] == "") return true; // 仮処理 モーション部が終わった後に改行とか入ってたとりあえず無視する。

						var channnel = node.Channnels[ch];
						float value = float.Parse(words[wordIndex]);
						switch (channnel)
						{
							case Node.Channel.Xposition: pos.X = value; break;
							case Node.Channel.Yposition: pos.Y = value; break;
							case Node.Channel.Zposition: pos.Z = value; break;
							case Node.Channel.Xrotation: rot.X = value; break;
							case Node.Channel.Yrotation: rot.Y = value; break;
							case Node.Channel.Zrotation: rot.Z = value; break;
						}
						wordIndex++;
					}
					node.SetMotionPos(motionNum, pos);
					node.SetMotionRot(motionNum, rot);
				}
				motionNum++;
			}

			return true;
		}

		/** --------------------------------------------
		 * @brief  セーブ
		 * @param  filepath	ファイルパス
		 * @return void
		 * --------------------------------------------*/
		public void Save(string filepath)
		{
			if (filepath == null) return;
			using (StreamWriter wr = new StreamWriter(filepath))
			{
				//!< 構造部
				WriteHierarchy(wr, _root);

				//!< モーション部
				WriteMotion(wr);
			}
		}

		/** --------------------------------------------
		 * @brief  ノード構造部の書き出し関数
		 * @param  wr	書き込みストリーム
		 * @param  node	ノード
		 * @return void
		 * --------------------------------------------*/
		private void WriteHierarchy(StreamWriter wr, Node root)
		{
			wr.WriteLine("HIERARCHY");
			wr.WriteLine("ROOT {0}", root.Name);
			wr.WriteLine("{");
			wr.WriteLine("\t" + GetStringOffset(root.Offset));
			wr.WriteLine("\t" + GetStringChannel(root.Channnels));

			if (root.Nodes.Count == 0)
			{
				// Rootしか無ければ。
				wr.WriteLine("\tEnd Site");
				wr.WriteLine("\t{");
				wr.WriteLine("\t\t" + GetStringOffset(root.Offset));
				wr.WriteLine("\t}");
			}
			else
			{
				foreach (var node in root.Nodes)
				{
					WriteJoint(wr, node, 1);
				}
			}
			wr.WriteLine("}");
		}

		/** --------------------------------------------
		 * @brief  ノードの書き出し
		 * @param  node	ノード
		 * @return void
		 * --------------------------------------------*/
		private void WriteJoint(StreamWriter wr, Node node, int layer)
		{
			//!< 階層を示すタブ(空白)追加
			string tab = "";
			for (int i = 0; i < layer; i++)
				tab += "\t";

			//!< ジョイント情報書き出し
			wr.WriteLine(tab + GetStringJointName(node.Name));
			wr.WriteLine(tab + "{");
			wr.WriteLine(tab + "\t" + GetStringOffset(node.Offset));
			wr.WriteLine(tab + "\t" + GetStringChannel(node.Channnels));


			if (node.Nodes.Count == 0)
			{
				//子がいない(=末端)
				wr.WriteLine(tab + "\tEnd Site");
				wr.WriteLine(tab + "\t{");
				wr.WriteLine(tab + "\t\t" + GetStringOffset(new Vec3(0, 0, 0)));
				wr.WriteLine(tab + "\t}");
			}
			else
			{
				foreach (var child in node.Nodes)
				{
					WriteJoint(wr, child, layer + 1);
				}
			}
			wr.WriteLine(tab + "}");
		}

		/** --------------------------------------------
		 * @brief  ジョイント名のStringを取得
		 * @param  name	ジョイント名
		 * @return string
		 * --------------------------------------------*/
		private string GetStringJointName(string name)
		{
			return "JOINT " + name;
		}

		/** --------------------------------------------
		 * @brief  オフセット情報のStringを取得
		 * @param  offset	オフセット値
		 * @return string
		 * --------------------------------------------*/
		private string GetStringOffset(Vec3 offset)
		{
			string buffer = "";
			buffer += "OFFSET ";
			buffer += offset.X.ToString();
			buffer += " ";
			buffer += offset.Y.ToString();
			buffer += " ";
			buffer += offset.Z.ToString();

			return buffer;
		}

		/** --------------------------------------------
		 * @brief  チャンネル情報のStringを取得
		 * @param  channels	チャンネルリスト
		 * @return string
		 * --------------------------------------------*/
		private string GetStringChannel(List<Node.Channel> channels)
		{
			string buffer = "CHANNELS " + channels.Count.ToString() + " ";

			foreach (var c in channels)
			{
				switch (c)
				{
					case Node.Channel.Xposition: buffer += "Xposition "; break;
					case Node.Channel.Yposition: buffer += "Yposition "; break;
					case Node.Channel.Zposition: buffer += "Zposition "; break;
					case Node.Channel.Xrotation: buffer += "Xrotation "; break;
					case Node.Channel.Yrotation: buffer += "Yrotation "; break;
					case Node.Channel.Zrotation: buffer += "Zrotation "; break;
				}
			}
			return buffer;
		}

		/** --------------------------------------------
		 * @brief  モーションの書き出し関数
		 * @param  wr	書き込みストリーム
		 * @return void
		 * --------------------------------------------*/
		private void WriteMotion(StreamWriter wr)
		{
			// HEADER
			wr.WriteLine("MOTION");
			wr.WriteLine("Frames: {0}", FrameNum);
			wr.WriteLine("Frame Time: {0}", FrameSpan);

			for (int frame = 0; frame < FrameNum; frame++)
			{
				string buffer = "";
				// 読み込み時に取得したノードの順番を使う
				foreach (var node in _nodeOrder)
				{
					// チャンネル順に書き込む必要がある
					foreach (var channel in node.Channnels)
					{
						var pos = node.GetMotionPos(frame);
						var rot = node.GetMotionRot(frame);

						switch (channel)
						{
							case Node.Channel.Xposition: buffer += pos.X.ToString(); break;
							case Node.Channel.Yposition: buffer += pos.Y.ToString(); break;
							case Node.Channel.Zposition: buffer += pos.Z.ToString(); break;
							case Node.Channel.Xrotation: buffer += rot.X.ToString(); break;
							case Node.Channel.Yrotation: buffer += rot.Y.ToString(); break;
							case Node.Channel.Zrotation: buffer += rot.Z.ToString(); break;
						}
						buffer += " ";
					}
				}// end node

				wr.WriteLine(buffer);　//1フレーム分読み終わったら出力
			}// end frame
			wr.WriteLine(); // ラストに改行
		}
	}
}
