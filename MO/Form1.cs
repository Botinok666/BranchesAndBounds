using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MathExtension;

namespace MO
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Rational[] record;
        private void Form1_Load(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
            button2_Click(sender, e);
        }

        private void PrintMatrix(Rational[,] mx)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int[] width = new int[mx.GetLength(0) - 1];
            for (int i = 1; i < mx.GetLength(0); i++)
            {
                width[i - 1] = 4;
                for (int j = 1; j < mx.GetLength(1); j++)
                    width[i - 1] = Math.Max(mx[i, j].ToString().Length, width[i - 1]);
            }
            stringBuilder.Append("Base|");
            for (int k = 1; k < mx.GetLength(0) - 1; k++)
                stringBuilder.AppendFormat("x{0,-" + (width[k - 1] - 1).ToString() + "}|", k.ToString());
            stringBuilder.Append("Res" + Environment.NewLine);
            for (int i = 1; i < mx.GetLength(1); i++)
            {
                if (mx[0, i] == 0) stringBuilder.Append("z   |");
                else stringBuilder.Append("x" + mx[0, i].ToString() + "  |");
                for (int j = 1; j < mx.GetLength(0); j++)
                    stringBuilder.Append(string.Format("{0,-" + width[j - 1].ToString() + "}|", mx[j, i].ToString()));
                stringBuilder.Append(Environment.NewLine);
            }
            textBox1.AppendText(stringBuilder.ToString());
        }
        /// <summary>
        /// Решение матрицы симплекс методом
        /// </summary>
        /// <param name="mx">Матрица, вид [столбцы, строки]. Содержит в 0 строке и 0 столбце индексы</param>
        /// <param name="minS"><code>true</code>, если ищем минимум</param>
        void Simplex(Rational[,] mx, bool minS)
        {
            while (true)
            {
                int columnIdx = 0; //разрешающий столбец
                Rational temp = Rational.Indeterminate; //минимальный элемент в строке z
                //Ищем разрешающий столбец (индекс)
                for (int i = 1; i < mx.GetLength(0) - 1; i++)
                {
                    //Условия для минимизации : максимизации
                    if ((Rational.IsIndeterminate(temp) && (minS ? mx[i, 1] > 0 : mx[i, 1] < 0)) ||
                        (!Rational.IsIndeterminate(temp) && (minS ? temp < mx[i, 1] : temp > mx[i, 1])))
                    {
                        columnIdx = i;
                        temp = mx[i, 1];
                    }
                }
                if (Rational.IsIndeterminate(temp)) break;
                //Ищем разешающую строку
                int rowIdx = 0;
                Rational temp2 = Rational.Indeterminate;
                for (int j = 2; j < mx.GetLength(1); j++) //Строки для всех S
                {
                    if (mx[columnIdx, j] > 0)
                    {
                        temp = mx[mx.GetLength(0) - 1, j] / mx[columnIdx, j];
                        if (Rational.IsIndeterminate(temp2) || temp2 > temp)
                        {
                            temp2 = temp; //Минимальное значение
                            rowIdx = j;
                        }
                    }
                }
                //Вводим переменную
                mx[0, rowIdx] = mx[columnIdx, 0];
                temp2 = mx[columnIdx, rowIdx];
                for (int i = 1; i < mx.GetLength(0); i++)
                    mx[i, rowIdx] /= temp2; //Делим на разрешающий элемент
                //Вычисляем остальные строки
                for (int j = 1; j < mx.GetLength(1); j++)
                {
                    if (j == rowIdx) continue;
                    temp2 = mx[columnIdx, j];
                    for (int i = 1; i < mx.GetLength(0); i++)
                        mx[i, j] -= mx[i, rowIdx] * temp2;
                }
            }
            PrintMatrix(mx); //Debug
        }
        /// <summary>
        /// Итерация метода ветвей и границ
        /// </summary>
        /// <param name="additionalRestrictions">Дополнительные ограничения, вводимые при ветвлении</param>
        private void Iterate(List<DataRow> additionalRestrictions)
        {
            DataTable dt = (dataGridView1.DataSource as DataTable).Copy();
            dt.Columns.RemoveAt(0);
            Dictionary<int, int> rSign = new Dictionary<int, int>();
            StringBuilder @string = new StringBuilder();
            foreach (var r in additionalRestrictions)
            {
                var x = dt.NewRow();
                x.ItemArray = r.ItemArray.Clone() as object[];
                for (int j = 0; j < r.ItemArray.Length; j++)
                {
                    if (!(x[j] is DBNull))
                    {
                        int sgn = Math.Sign(((Rational)x[j]).ToDouble());
                        if (sgn != 0 && j < x.ItemArray.Length - 1)
                        {
                            rSign.Add(dt.Rows.Count, sgn);
                            @string.AppendFormat(" x{0}{1}{2}", j + 1,
                                sgn > 0 ? '≤' : '≥',
                                Rational.Abs((Rational)x[x.ItemArray.Length - 1]));
                        }
                        x[j] = Rational.Abs((Rational)x[j]);
                    }
                }
                dt.Rows.Add(x);
            }

            int xcnt = dt.Columns.Count - 1;
            int scnt = dt.Rows.Count - 1;
            Rational[,] matrix = new Rational[xcnt + scnt + 2, scnt + 2];
            for (int j = 0; j < matrix.GetLength(0); j++) //Столбцы
                matrix[j, 0] = j;
            for (int i = 1; i < matrix.GetLength(1); i++) //Строки
            {
                matrix[0, i] = i == 1 ? 0 : i + xcnt - 1;
                for (int j = 1; j < matrix.GetLength(0); j++) //Столбцы
                {
                    if (j < dt.Columns.Count) //Заполним для всех Х
                    {
                        object txx = dt.Rows[i - 1][j - 1];
                        matrix[j, i] = txx is DBNull ? 0 : (Rational)txx;
                    }
                    else if (matrix[0, i] == matrix[j, 0]) //Для всех S единички по диагонали
                    {
                        matrix[j, i] = rSign.ContainsKey(i) ? rSign[i] : 1;
                    }
                    else if (j == matrix.GetLength(0) - 1)
                    {
                        object txx = dt.Rows[i - 1][dt.Columns.Count - 1];
                        matrix[j, i] = txx is DBNull ? 0 : (Rational)txx;
                    }
                    else
                        matrix[j, i] = 0;
                }
            }
            //PrintMatrix(matrix);
            textBox1.AppendText(string.Format("\r\nРешение{0}:{1}", @string.ToString(), Environment.NewLine));
            Simplex(matrix, checkBox1.Checked);
            Rational[] result = new Rational[xcnt];
            for (int i = 0; i < result.Length; i++)
                result[i] = 0;
            for (int i = 2; i < matrix.GetLength(1); i++)
            {
                if (matrix[0, i] <= xcnt)
                    result[matrix[0, i].Numerator - 1] = matrix[matrix.GetLength(0) - 1, i];
            }
            int rIdx = -1;
            for (int i = 0; i < result.Length; i++)
            {
                textBox1.AppendText(string.Format("x{0}={1}{2}", i + 1, result[i].ToMixedString(),
                    i != result.Length - 1 ? "; " : "  "));
                if (rIdx < 0 && result[i].Denominator > 1) //Дробный корень
                    rIdx = i;
            }
            textBox1.AppendText(string.Format("z(max)= {2}   {0} решение{1}", rIdx < 0 && matrix[matrix.GetLength(0) - 1, 1].Denominator == 1
                ? "Целое" : "Дробное", Environment.NewLine,
                matrix[matrix.GetLength(0) - 1, 1].ToMixedString()));
            if (rIdx < 0)
            {
                if (matrix[matrix.GetLength(0) - 1, 1].Denominator != 1)
                {
                    textBox1.AppendText("Нет решения" + Environment.NewLine);
                    return;
                }
                for (int i = 2; i < dataGridView1.RowCount; i++)
                {
                    Rational check = 0;
                    for (int j = 1; j < dataGridView1.Columns.Count - 1; j++)
                    {
                        check += (result[j - 1] * (Rational)(dataGridView1[j, i].Value is DBNull ?
                            Rational.Zero : dataGridView1[j, i].Value));
                    }
                    if (check > (Rational)dataGridView1[dataGridView1.ColumnCount - 1, i].Value)
                    {
                        textBox1.AppendText("Ограничения не выполнены" + Environment.NewLine);
                        return;
                    }
                }
                if (checkBox1.Checked == false)
                {
                    if (matrix[matrix.GetLength(0) - 1, 1] > record[record.Length - 1])
                    {
                        for (int i = 0; i < record.Length - 1; i++)
                        {
                            record[i] = result[i];
                        }
                        record[record.Length - 1] = matrix[matrix.GetLength(0) - 1, 1];
                    }
                }
                else
                {
                    if (matrix[matrix.GetLength(0) - 1, 1] < record[record.Length - 1] || record[record.Length - 1] == 0)
                    {
                        for (int i = 0; i < record.Length - 1; i++)
                        {
                            record[i] = result[i];
                        }
                        record[record.Length - 1] = matrix[matrix.GetLength(0) - 1, 1];
                    }
                }
            }
            if (rIdx >= 0 && xcnt > additionalRestrictions.Count)
            {
                foreach (var a in additionalRestrictions)
                {
                    if (!(a[rIdx] is DBNull) && Rational.Abs((Rational)a[rIdx]) == 1)
                        return; //Уже есть ограничение для этого Х
                }
                additionalRestrictions.Add(dt.NewRow());
                additionalRestrictions[additionalRestrictions.Count - 1][rIdx] = Rational.One;
                additionalRestrictions[additionalRestrictions.Count - 1][dt.Columns.Count - 1] =
                    (Rational)Math.Floor(result[rIdx].ToDouble());
                Iterate(additionalRestrictions);
                additionalRestrictions[additionalRestrictions.Count - 1][rIdx] = -Rational.One;
                additionalRestrictions[additionalRestrictions.Count - 1][dt.Columns.Count - 1] =
                    -(Rational)Math.Ceiling(result[rIdx].ToDouble());
                Iterate(additionalRestrictions);
                additionalRestrictions.RemoveAt(additionalRestrictions.Count - 1);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            Iterate(new List<DataRow>());
            if (record[record.Length - 1] == 0)
            {
                textBox2.Text = "Нет оптимального решения!!!!!!!!!!!!!";
            }
            else
            {
                textBox2.Clear();
                for (int i = 0; i < record.Length - 1; i++)
                {
                    textBox2.Text += "x" + (i + 1).ToString() + "=" + record[i].ToString() + "  ";
                }
                textBox2.Text += "Z(max)=" + record[record.Length - 1];
            }
        }
        //задает таблицу
        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
            textBox2.Clear();
            dataGridView1.DataSource = null;
            DataTable dt = new DataTable();
            dt.Columns.Add("Base");
            dt.Columns[0].ReadOnly = true;
            for (int i = 1; i <= numericUpDown1.Value; i++)
            {
                dt.Columns.Add("x" + i.ToString(), typeof(Rational));
            }
            dt.Columns.Add("res", typeof(Rational));
            dt.Rows.Add("z");
            for (int i = 1; i <= numericUpDown2.Value; i++)
            {
                dt.Rows.Add("x" + (i + numericUpDown1.Value).ToString());
            }
            dataGridView1.DataSource = dt;
            record = new Rational[(int)numericUpDown1.Value + 1];
        }
    }
}
