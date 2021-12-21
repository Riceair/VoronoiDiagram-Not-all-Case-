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
        private Queue<List<PointF>> points_list_buffer = new Queue<List<PointF>>(); //儲存input分次的點
        private Queue<List<PointEdgeRecoder>> record_buffer = new Queue<List<PointEdgeRecoder>>();
        private List<PointF> points_list = new List<PointF>(); //儲存要處理的points
        private List<string> input_lines = new List<string>(); //儲存讀檔後的每一列
        private DiagramCalculator diagramCalculator; //運算Voroni Diagram的物件
        PointEdgeRecoder result_recod;
        private bool isReadOnly = false; //是否可以從畫布加點(true表示只能讀，不能加點)
        private bool isRunAll = false; //是否按全部執行
        private string write_path = "output.out";
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

            diagramCalculator = new DiagramCalculator(pictureBox1.Width);
            diagramCalculator.setTextBox(textBox1); //Debug

            //C#的垃圾BUG 以下行畫不出來
            //DrawLine(new PointF((float)150.0039, 150), new PointF(150, -62900));
        }

        private void inputToolStripMenuItem_Click(object sender, EventArgs e) //開input檔案
        {
            reset();

            try
            {
                openFileDialog1.Title = "Open Input";
                if (openFileDialog1.ShowDialog() == DialogResult.OK) //取得選擇的檔名
                    readFile(openFileDialog1.FileName);

                int point_count = 0; //紀錄輸入點數目
                List<PointF> pList = new List<PointF>();
                foreach(string line in input_lines)
                {
                    if(point_count==0)
                    {
                        point_count = Convert.ToInt32(line);
                        if(point_count == 0)
                            break; //若收到的輸入是0，直接break
                        pList = new List<PointF>();
                    }
                    else
                    {
                        point_count--;
                        float x = (float) Convert.ToDouble(line.Split(' ')[0]); //取得x y座標
                        float y = (float) Convert.ToDouble(line.Split(' ')[1]);
                        pList.Add(new PointF(x, y));
                        if(point_count==0) //加完點了，把pList加入buffer
                            points_list_buffer.Enqueue(pList);
                    }
                }
                points_list = points_list_buffer.Dequeue(); //先將第一筆取出來
                DrawListPoints();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void outputToolStripMenuItem_Click(object sender, EventArgs e) //開output檔案
        {
            reset();

            try
            {
                openFileDialog1.Title = "Open Output";
                if (openFileDialog1.ShowDialog() == DialogResult.OK) //取得選擇的檔名
                    readFile(openFileDialog1.FileName);

                List<PointF> pList = new List<PointF>();
                List<Edge> eList = new List<Edge>();
                foreach(string line in input_lines){
                    string[] line_split = line.Split(' ');
                    if(line_split[0].Equals("P"))
                        pList.Add(new PointF((float)Convert.ToDouble(line_split[1]), (float)Convert.ToDouble(line_split[2])));
                    if(line_split[0].Equals("E")){
                        PointF p1 = new PointF((float)Convert.ToDouble(line_split[1]), (float)Convert.ToDouble(line_split[2]));
                        PointF p2 = new PointF((float)Convert.ToDouble(line_split[3]), (float)Convert.ToDouble(line_split[4]));
                        eList.Add(new Edge(p1, p2));
                    }
                }
                result_recod = new PointEdgeRecoder(pList, eList);
                points_list = pList;
                DrawListPoints();
                foreach(Edge edge in result_recod.edges_list)
                    DrawLine(edge.edgePA, edge.edgePB);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(!isRunAll) return; //還未取得執行結果

            List<PointF> pList = result_recod.points_list; //取得result的point
            List<Edge> eList = result_recod.edges_list; //取得result的edge
            StreamWriter sw;
            // FileStream fs = new FileStream(write_path,
            //                 FileMode.Append, FileAccess.Write);

            saveFileDialog1.Title = "Save Output";
            if(saveFileDialog1.ShowDialog() == DialogResult.OK){
                write_path = saveFileDialog1.FileName;
            }
            else return;

            using(sw = new StreamWriter(write_path))
            {
                foreach(PointF point in pList)
                    sw.WriteLine("P "+point.X+" "+point.Y);
                foreach(Edge edge in eList)
                    sw.WriteLine("E "+edge.edgePA.X+" "+edge.edgePA.Y+" "+edge.edgePB.X+" "+edge.edgePB.Y);
            }
            sw.Close();
        }

        private void nextToolStripMenuItem_Click(object sender, EventArgs e) //若有下一個顯示下一個，若沒有清空畫布
        {
            if(points_list_buffer.Count()>0) //若buffer有資料
            {
                clear();
                points_list = points_list_buffer.Dequeue();
                record_buffer.Clear();
                DrawListPoints();
            }
            else{
                reset();
            }
        }

        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(points_list.Count==0) return;
            if(!isRunAll || record_buffer.Count == 0){
                isReadOnly = true;
                isRunAll = true;
                result_recod = diagramCalculator.run(points_list);
                record_buffer = diagramCalculator.getRecoderBuffer();
            }
            
            clear();
            DrawListPoints();
            List<PointEdgeRecoder> record_list = record_buffer.Dequeue();
            foreach(PointEdgeRecoder record in record_list)
            {
                foreach(PointF point in record.points_list)
                    DrawPointF(point, record.point_color);
                foreach(Edge edge in record.edges_list)
                    DrawLine(edge.edgePA, edge.edgePB, record.edge_color);
                foreach(Edge edge in record.convex_list)
                    DrawLine(edge.edgePA, edge.edgePB, record.convex_color);
                foreach(Edge edge in record.hyper_list)
                    DrawLine(edge.edgePA, edge.edgePB, record.hyper_color);
            }
        }
        private void runToolStripMenuItem_Click(object sender, EventArgs e) //跑執行全部
        {
            if(points_list.Count==0) return;
            isReadOnly = true;
            isRunAll = true;
            record_buffer.Clear();
            clear();
            DrawListPoints();
            result_recod = diagramCalculator.run(points_list);
            record_buffer = diagramCalculator.getRecoderBuffer();
            foreach(Edge edge in result_recod.edges_list)
                DrawLine(edge.edgePA, edge.edgePB);
            foreach(Edge edge in result_recod.convex_list)
                DrawLine(edge.edgePA, edge.edgePB, result_recod.convex_color);
        }

        private void reset(){
            points_list_buffer.Clear(); //清空buffer
            points_list.Clear(); //清空紀錄的point
            isReadOnly = false;
            isRunAll = false;
            record_buffer.Clear();
            clear();
        }

        private void clear(){
            textBox1.Text = "";
            graphics.Clear(Color.White);
            graphics.DrawImage(bmp, 0, 0);
            pictureBox1.Image = bmp;
            pictureBox1.Refresh();
        }

        private void readFile(string filename){
            StreamReader sr;
            string line = "";

            input_lines.Clear();
            using (sr = new StreamReader(filename))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("#") || line.Equals("")) //不處理註解
                        continue;
                    input_lines.Add(line); //紀錄輸入
                }
            }
            sr.Close();
        }

        private void DrawListPoints(){
            foreach(PointF point in points_list)
                DrawPointF(point);
            isReadOnly = true;
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
        }

        private void DrawPointF(PointF pointF){ //畫點
            Brush brush = new SolidBrush(Color.DarkGray); //設定brush
            RectangleF rectangleF = new RectangleF(pointF.X-3, pointF.Y-3, POINT_SIZE, POINT_SIZE); //畫長方形(由於初始化設定，畫出來是圓)
            graphics.FillEllipse(brush, rectangleF); //在graphic上作畫(會畫在bmp上)
            pictureBox1.Image = bmp; //pictureBox更新bmp
            textBox1.AppendText("("+pointF.X+", "+pointF.Y+")\r\n"); //convertAxisY(p.Y)
        }

        private void DrawPointF(PointF pointF, Color color){ //畫點
            Brush brush = new SolidBrush(color); //設定brush
            RectangleF rectangleF = new RectangleF(pointF.X-3, pointF.Y-3, POINT_SIZE, POINT_SIZE); //畫長方形(由於初始化設定，畫出來是圓)
            graphics.FillEllipse(brush, rectangleF); //在graphic上作畫(會畫在bmp上)
            pictureBox1.Image = bmp; //pictureBox更新bmp
        }

        private void DrawLine(PointF point1, PointF point2){ //畫線
            Pen pen = new Pen(Color.Gray, LINE_WIDTH);
            graphics.DrawLine(pen, point1, point2);
            pictureBox1.Image = bmp;
        }

        private void DrawLine(PointF point1, PointF point2, Color color){ //畫線
            Pen pen = new Pen(color, LINE_WIDTH);
            graphics.DrawLine(pen, point1, point2);
            pictureBox1.Image = bmp;
        }
    }
}
