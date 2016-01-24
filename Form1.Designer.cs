namespace BVHViewer
{
	partial class BVHViewer
	{
		/// <summary>
		/// 必要なデザイナー変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナーで生成されたコード

		/// <summary>
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this.glControl1 = new OpenTK.GLControl();
			this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.cb_joint_axis = new System.Windows.Forms.CheckBox();
			this.cb_joint_pos = new System.Windows.Forms.CheckBox();
			this.cb_parent_line = new System.Windows.Forms.CheckBox();
			this.label_fps = new System.Windows.Forms.Label();
			this.trackBar1 = new System.Windows.Forms.TrackBar();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
			this.SuspendLayout();
			// 
			// glControl1
			// 
			this.glControl1.AllowDrop = true;
			this.glControl1.BackColor = System.Drawing.Color.Black;
			this.glControl1.Location = new System.Drawing.Point(122, 12);
			this.glControl1.Name = "glControl1";
			this.glControl1.Size = new System.Drawing.Size(919, 587);
			this.glControl1.TabIndex = 0;
			this.glControl1.VSync = false;
			this.glControl1.DragDrop += new System.Windows.Forms.DragEventHandler(this.glControl1_DragDrop);
			this.glControl1.DragEnter += new System.Windows.Forms.DragEventHandler(this.glControl1_DragEnter);
			this.glControl1.MouseEnter += new System.EventHandler(this.glControl1_MouseEnter);
			this.glControl1.MouseLeave += new System.EventHandler(this.glControl1_MouseLeave);
			// 
			// button1
			// 
			this.button1.AllowDrop = true;
			this.button1.Location = new System.Drawing.Point(13, 13);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(103, 58);
			this.button1.TabIndex = 1;
			this.button1.Text = "Load";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			this.button1.DragDrop += new System.Windows.Forms.DragEventHandler(this.dragDrop);
			this.button1.DragEnter += new System.Windows.Forms.DragEventHandler(this.dragEnter);
			// 
			// button2
			// 
			this.button2.AllowDrop = true;
			this.button2.Location = new System.Drawing.Point(13, 77);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(103, 58);
			this.button2.TabIndex = 2;
			this.button2.Text = "Play";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(19, 160);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(47, 12);
			this.label1.TabIndex = 3;
			this.label1.Text = "Frame : ";
			// 
			// cb_joint_axis
			// 
			this.cb_joint_axis.AutoSize = true;
			this.cb_joint_axis.Location = new System.Drawing.Point(13, 224);
			this.cb_joint_axis.Name = "cb_joint_axis";
			this.cb_joint_axis.Size = new System.Drawing.Size(71, 16);
			this.cb_joint_axis.TabIndex = 4;
			this.cb_joint_axis.Text = "joint_axis";
			this.cb_joint_axis.UseVisualStyleBackColor = true;
			// 
			// cb_joint_pos
			// 
			this.cb_joint_pos.AutoSize = true;
			this.cb_joint_pos.Checked = true;
			this.cb_joint_pos.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cb_joint_pos.Location = new System.Drawing.Point(13, 202);
			this.cb_joint_pos.Name = "cb_joint_pos";
			this.cb_joint_pos.Size = new System.Drawing.Size(68, 16);
			this.cb_joint_pos.TabIndex = 5;
			this.cb_joint_pos.Text = "joint_pos";
			this.cb_joint_pos.UseVisualStyleBackColor = true;
			// 
			// cb_parent_line
			// 
			this.cb_parent_line.AutoSize = true;
			this.cb_parent_line.Checked = true;
			this.cb_parent_line.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cb_parent_line.Location = new System.Drawing.Point(13, 247);
			this.cb_parent_line.Name = "cb_parent_line";
			this.cb_parent_line.Size = new System.Drawing.Size(78, 16);
			this.cb_parent_line.TabIndex = 6;
			this.cb_parent_line.Text = "parent_line";
			this.cb_parent_line.UseVisualStyleBackColor = true;
			// 
			// label_fps
			// 
			this.label_fps.AutoSize = true;
			this.label_fps.Location = new System.Drawing.Point(22, 144);
			this.label_fps.Name = "label_fps";
			this.label_fps.Size = new System.Drawing.Size(28, 12);
			this.label_fps.TabIndex = 7;
			this.label_fps.Text = "FPS:";
			// 
			// trackBar1
			// 
			this.trackBar1.Location = new System.Drawing.Point(12, 175);
			this.trackBar1.Maximum = 1;
			this.trackBar1.Name = "trackBar1";
			this.trackBar1.Size = new System.Drawing.Size(104, 45);
			this.trackBar1.TabIndex = 8;
			this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.None;
			this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
			// 
			// BVHViewer
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1053, 611);
			this.Controls.Add(this.glControl1);
			this.Controls.Add(this.label_fps);
			this.Controls.Add(this.cb_parent_line);
			this.Controls.Add(this.cb_joint_pos);
			this.Controls.Add(this.cb_joint_axis);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.trackBar1);
			this.Name = "BVHViewer";
			this.Text = "BVH(+α)Viewer";
			this.Shown += new System.EventHandler(this.Form1_Shown);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.dragDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.dragEnter);
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private OpenTK.GLControl glControl1;
		private System.ComponentModel.BackgroundWorker backgroundWorker1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox cb_joint_axis;
		private System.Windows.Forms.CheckBox cb_joint_pos;
		private System.Windows.Forms.CheckBox cb_parent_line;
		private System.Windows.Forms.Label label_fps;
		private System.Windows.Forms.TrackBar trackBar1;
	}
}

