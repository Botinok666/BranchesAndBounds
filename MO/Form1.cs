using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MO
{
    public partial class Form1 : Form
    {
        const int MV = int.MaxValue;
        int[,] matrix = new int[6, 6] { 
            { 0,  1,  2,  3,  4,  5 }, 
            { 1, MV, 25, 40, 31, 27 }, 
            { 2,  5, MV, 17, 30, 25 },
            { 3, 19, 15, MV,  6,  1 },
            { 4,  9, 50, 24, MV,  6 },
            { 5, 22,  8,  7, 10, MV } };

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Leaf[] temp = new Leaf[matrix.GetLength(0) - 1];
            Leaf[] result = new Leaf[temp.Length];
            int record = int.MaxValue;
            iterate(matrix, 0, ref record, temp, result);
            textBox1.AppendText(string.Format("\nРекорд: {0}\n", record));
            for (int i = 0; i < result.Length; i++)
                textBox1.AppendText(string.Format("{0}→{1}{2}", result[i].row, result[i].column,
                    i + 1 == result.Length ? Environment.NewLine : "; "));
        }
        private void PrintMatrix(int[,] mx)
        {
            textBox1.AppendText(string.Format("Порядок: {0}\n", mx.GetLength(0) - 1));
            for (int i = 1; i < mx.GetLength(0); i++)
            {
                for (int j = 1; j < mx.GetLength(1); j++)
                    textBox1.AppendText((mx[i, j] == int.MaxValue ? "∞" : mx[i, j].ToString()) + '\t');
                textBox1.AppendText(Environment.NewLine);
            }
        }
        /// <summary>
        /// Итерация метода ветвей и границ
        /// </summary>
        /// <param name="mx">Матрица расстояний, вид [строки, столбцы]. Содержит в 0 строке и 0 столбце индексы</param>
        /// <param name="phi">Текущая нижняя оценка</param>
        /// <param name="phiMin">Лучшая нижняя оценка</param>
        /// <param name="tempPath">Текущий маршрут, состоящий из набора переходов. 
        /// Должен быть инициализирован, размер равен порядку матрицы</param>
        /// <param name="resultPath">Лучший маршрут, состоящий из набора переходов. 
        /// Должен быть инициализирован, размер равен порядку матрицы</param>
        void iterate(int[,] mx, int phi, ref int phiMin, Leaf[] tempPath, Leaf[] resultPath)
        {
            PrintMatrix(mx); //Debug
            for (int i = 1; i < mx.GetLength(0); i++) //Минимизация по строкам
            {
                int t = int.MaxValue, j;
                for (j = 1; j < mx.GetLength(1); j++)
                    t = Math.Min(t, mx[i, j]);
                for (j = 1; j < mx.GetLength(1); j++)
                    if (mx[i, j] != int.MaxValue)
                        mx[i, j] -= t;
                phi += t;
            }
            for (int j = 1; j < mx.GetLength(1); j++) //Минимизация по столбцам
            {
                int t = int.MaxValue, i;
                for (i = 1; i < mx.GetLength(0); i++)
                    t = Math.Min(t, mx[i, j]);
                for (i = 1; i < mx.GetLength(1); i++)
                    if (mx[i, j] != int.MaxValue)
                        mx[i, j] -= t;
                phi += t;
            }
            if (phi >= phiMin)
                return; //Нижняя оценка превышена
            textBox1.AppendText("ϕ=" + phi.ToString() + Environment.NewLine);
            List<Leaf> leafs = new List<Leaf>();
            for (int i = 1; i < mx.GetLength(0); i++) //Поиск всех нулей
            {
                for (int j = 1; j < mx.GetLength(1); j++)
                    if (mx[i, j] == 0)
                        leafs.Add(new Leaf()
                        {
                            row = mx[i, 0],
                            column = mx[0, j],
                            rowIdx = i,
                            columnIdx = j,
                            delta = 0
                        });
            }
            for (int k = 0; k < leafs.Count; k++) //Ищем дельты для всех нулей
            {
                var x = leafs[k];
                int t = int.MaxValue;
                for (int i = 1; i < mx.GetLength(0); i++) //Ищем минимум по столбцу
                {
                    if (i == x.rowIdx) continue;
                    t = Math.Min(t, mx[i, x.columnIdx]);
                }
                if (t == int.MaxValue)
                    t = 0; //Если в столбце кроме рассматриваемого нуля и бесконечностей ничего не осталось
                x.delta += t;
                t = int.MaxValue;
                for (int j = 1; j < mx.GetLength(1); j++) //Теперь по строке
                {
                    if (j == x.columnIdx) continue;
                    t = Math.Min(t, mx[x.rowIdx, j]);
                }
                if (t == int.MaxValue)
                    t = 0;
                x.delta += t;
                leafs[k] = x;
            }
            int stepIdx = mx.GetLength(0) - 2; //Индекс в массиве с сохранёнными шагами
            int chkCnt = tempPath.Length - stepIdx;
            if (stepIdx == 0) //Матрица размером 2*2?
            {
                if (phi + leafs[0].delta < phiMin)
                {
                    phiMin = phi + leafs[0].delta;
                    Array.Copy(tempPath, resultPath, tempPath.Length);
                    resultPath[0].column = leafs[0].column;
                    resultPath[0].row = leafs[0].row;
                    textBox1.AppendText("Новый рекорд: " + phiMin.ToString() + Environment.NewLine);
                }
                return;
            }

            int[,] temp = new int[mx.GetLength(0) - 1, mx.GetLength(1) - 1];
            leafs.Sort();
            for (int k = 0; k < leafs.Count; k++)
            {
                var x = leafs[k];
                if (k > 0 && leafs[0].delta + phi >= phiMin)
                {
                    textBox1.AppendText("Нижняя граница превышена" + Environment.NewLine);
                    break;
                }
                tempPath[stepIdx].column = x.column; //Запомним сделанный шаг
                tempPath[stepIdx].row = x.row;
                if (chkCnt > 2) //Начиная с 3 шага проверим на досрочное замыкание маршрута
                {
                    int conn = 0; //Подсчёт числа отрезков маршрута, которые соединяются с другими
                    for (int i = stepIdx; i < tempPath.Length; i++)
                    {
                        for (int j = stepIdx; j < tempPath.Length; j++)
                        {
                            if (tempPath[i].column == tempPath[j].row)
                            {
                                conn++; //Один отрезок соединяется с другим
                                break;
                            }
                        }
                    }
                    if (conn == chkCnt) continue; //Число соединений совпало с числом отрезков маршрута
                }
                int a = 0;
                for (int i = 0; i < mx.GetLength(0); i++) //Заполняем новую матрицу с учётом перемещения
                {
                    if (i == x.rowIdx) continue; //Пропустим нужную строку
                    int b = 0;
                    for (int j = 0; j < mx.GetLength(1); j++)
                    {
                        if (j == x.columnIdx) continue; //Пропустим нужный столбец
                        temp[a, b++] = (mx[i, 0] != x.column || mx[0, j] != x.row) ?
                            mx[i, j] :
                            int.MaxValue; //Запретим для проверки обратное перемещение
                    }
                    a++;
                }
                iterate(temp, phi, ref phiMin, tempPath, resultPath); //Рекурсия
                if (phi >= phiMin) break; //После выполнения предыдущей строки все ветви могут быть отсечены
            }
        }
    }
    public struct Leaf : IComparable<Leaf>
    {
        public int row, column, rowIdx, columnIdx, delta;
        public int CompareTo(Leaf l) { return l.delta.CompareTo(delta); }
    }
}
