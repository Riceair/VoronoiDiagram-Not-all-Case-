using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VoronoiDiagram
{
    public partial class Form1 : Form
    {
        private List<PointF> points_list = new List<PointF>(); //儲存要處理的points
        private List<string> input_lines = new List<string>(); //儲存讀檔後的每一列
        private DiagramCalculator diagramCalculator; //運算Voroni Diagram的物件
        private int read_type; //0: 讀input檔, 1: 讀output檔
        private bool isReadOnly = false; //是否可以從畫布加點(true表示只能讀，不能加點)
        private bool isRunAll = false; //是否按全部執行
        private static int POINT_SIZE = 6; //點大小
        private static int LINE_WIDTH = 1; //線寬度
        Graphics graphics;
        Bitmap bmp;

        public Form1()
        {
            InitializeComponent();
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height); //建立bmp圖(graphics會在上面作圖)
            graphics = Graphics.FromImage(bmp); //建立graphics
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality; //設定點擊是畫圓
            graphics.Clear(Color.White);
            graphics.DrawImage(bmp, 0, 0);
            pictureBox1.Image = bmp; //將pictureBox設定畫好的graphic
            pictureBox1.Refresh();

            diagramCalculator = new DiagramCalculator(pictureBox1.Width, pictureBox1.Height);
            diagramCalculator.setTextBox(textBox1); //Debug
        }

        private void inputToolStripMenuItem_Click(object sender, EventArgs e) //開input檔案
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK) //取得選擇的檔名
                    readFile(openFileDialog1.FileName);
                read_type = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void outputToolStripMenuItem_Click(object sender, EventArgs e) //開output檔案
        {
            try
            {
                if (openFileDialog1.ShowDialog() == DialogResult.OK) //取得選擇的檔名
                    readFile(openFileDialog1.FileName);
                read_type = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void nextToolStripMenuItem_Click(object sender, EventArgs e) //若有下一個顯示下一個，若沒有清空畫布
        {
            points_list.Clear(); //清空紀錄的point
            graphics.Clear(Color.White);
            graphics.DrawImage(bmp, 0, 0);
            pictureBox1.Image = bmp;
            pictureBox1.Refresh();
            textBox1.Text = "";
            isReadOnly = false;
            isRunAll = false;
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e) //跑執行全部
        {
            if (isRunAll) return;

            isReadOnly = true;
            isRunAll = true;
            PointEdgeRecoder pointEdgeRecoder = diagramCalculator.run(points_list);
            foreach(Edge edge in pointEdgeRecoder.getEdges())
            {
                DrawLine(edge.edgePA, edge.edgePB);
            }
        }

        private void readFile(string filename){
            StreamReader sr;
            string line = "";

            input_lines.Clear();
            using (sr = new StreamReader(filename))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("#")) //不處理註解
                        continue;
                    input_lines.Add(line); //紀錄輸入
                }
            }
            sr.Close();
        }

        private float convertAxisY(float y){ //畫布Y座標和實際Y座標轉換(主要是顯示或紀錄用，實際運算會用畫布的Y座標)
            return pictureBox1.Height - y;
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e) //點擊畫布
        {
            if(isReadOnly) return; //readonly，不能加點

            PointF p = new PointF(e.X, e.Y); //取得當前event的X,Y
            points_list.Add(p); //記錄到list中
            DrawPointF(p); //畫點
            textBox1.AppendText("("+p.X+", "+p.Y+")\r\n"); //convertAxisY(p.Y)
        }

        private void DrawPointF(PointF pointF){ //畫點
            Brush brush = new SolidBrush(Color.Red); //設定brush
            RectangleF rectangleF = new RectangleF(pointF.X-3, pointF.Y-3, POINT_SIZE, POINT_SIZE); //畫長方形(由於初始化設定，畫出來是圓)
            graphics.FillEllipse(brush, rectangleF); //在graphic上作畫(會畫在bmp上)
            pictureBox1.Image = bmp; //pictureBox更新bmp
        }

        private void DrawLine(PointF point1, PointF point2){ //畫線
            Pen pen = new Pen(Color.Blue, LINE_WIDTH);
            graphics.DrawLine(pen, point1, point2);
            pictureBox1.Image = bmp;
        }
    }
}
