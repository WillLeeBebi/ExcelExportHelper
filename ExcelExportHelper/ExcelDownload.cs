﻿using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;

namespace ExcelExportHelper
{
    /// <summary>
    /// 使用Npoi导出Excel公共类
    /// </summary>
    public class ExcelDownload
    {
        private readonly string excelName;
        private readonly string excelSheetName;
        private IWorkbook HssfWork { get; set; }
        private ISheet HssfSheet { get; set; }
        private ExcelStyleMessage StyleMessage { get; set; }

        /// <summary>
        /// 初始化Excel相关信息
        /// </summary>
        /// <param name="ExcelName">Excel名称</param>
        /// <param name="ExcelSheetName">初始页签名称</param>
        public ExcelDownload(string ExcelName, string ExcelSheetName)
        {
            excelName = ExcelName;
            excelSheetName = ExcelSheetName;
            HssfWork = new HSSFWorkbook();
            HssfSheet = HssfWork.CreateSheet(excelSheetName);
            StyleMessage = new ExcelStyleMessage();
        }

        /// <summary>
        /// 导出Excel
        /// </summary>
        /// <param name="list">导出模型数据</param>
        public void ExportExcel<T>(IEnumerable<T> list)
        {
            GenerateExcel(list);
            DownLoadExcel();
        }
        /// <summary>
        /// 导出Excel
        /// </summary>
        /// <param name="excelSetMethod">操作Excel方法</param>
        public void ExportExcel(Action<IWorkbook, ISheet> excelSetMethod)
        {
            excelSetMethod.Invoke(HssfWork, HssfSheet);
            DownLoadExcel();
        }

        /// <summary>
        /// 生成Excel
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="list">数据集合</param>
        private void GenerateExcel<T>(IEnumerable<T> list)
        {
            if (list == null)
            {
                return;
            }
            CellStyleMethod.Workbook = HssfWork;
            Dictionary<PropertyInfo, ExcelInfoAttribute> _excelInfos = GetPropInfo<T>();
            SetExcelTitle(_excelInfos);
            SetExcelContent(list, _excelInfos);
        }

        /// <summary>
        /// 设置Excel内容
        /// </summary>
        /// <typeparam name="T">Excel模型实体</typeparam>
        /// <param name="list">Excel数据集合</param>
        /// <param name="excelInfos"></param>
        private void SetExcelContent<T>(IEnumerable<T> list, Dictionary<PropertyInfo, ExcelInfoAttribute> excelInfos)
        {
            int _rowNum = 1;
            Dictionary<string, ICellStyle> cellStyleList = new Dictionary<string, ICellStyle>();
            foreach (T rowItem in list)
            {
                int _rowCell = 0;
                IRow _rowValue = HssfSheet.CreateRow(_rowNum);
                foreach (var cellItem in excelInfos)
                {
                    object _cellItemValue = cellItem.Key.GetValue(rowItem, null);
                    ICell _cell = _rowValue.CreateCell(_rowCell);
                    if (!cellStyleList.ContainsKey(cellItem.Value.ExcelStyle.ToString()))
                    {
                        ICellStyle _cellStyle = StyleMessage.GetCellStyle(HssfWork, cellItem.Value.ExcelStyle);
                        cellStyleList.Add(cellItem.Value.ExcelStyle.ToString(), _cellStyle);
                    }
                    SetCellValue(cellItem, _cellItemValue, _cell);
                    _cell.CellStyle = cellStyleList[cellItem.Value.ExcelStyle.ToString()];
                    _rowCell++;
                }
                _rowNum++;
            }
        }

        /// <summary>
        /// 设置单元格内容
        /// </summary>
        private void SetCellValue(KeyValuePair<PropertyInfo, ExcelInfoAttribute> cellItem, object _cellItemValue, ICell cell)
        {
            string cellItemValue = _cellItemValue == null ? "" : _cellItemValue.ToString();
            switch (cellItem.Key.PropertyType.ToString())
            {
                case "System.String"://字符串类型   
                    cell.SetCellValue(cellItemValue);
                    break;
                case "System.DateTime"://日期类型   
                    DateTime dateV;
                    DateTime.TryParse(cellItemValue, out dateV);
                    cell.SetCellValue(dateV);
                    break;
                case "System.Boolean"://布尔型   
                    bool boolV = false;
                    bool.TryParse(cellItemValue, out boolV);
                    cell.SetCellValue(boolV);
                    break;
                case "System.Int16"://整型   
                case "System.Int32":
                case "System.Int64":
                case "System.Byte":
                    int intV = 0;
                    int.TryParse(cellItemValue, out intV);
                    cell.SetCellValue(intV);
                    break;
                case "System.Decimal"://浮点型   
                case "System.Double":
                    double doubV = 0;
                    double.TryParse(cellItemValue, out doubV);
                    cell.SetCellType(CellType.Numeric);
                    cell.SetCellValue(doubV);
                    break;
                case "System.DBNull"://空值处理   
                    cell.SetCellValue("");
                    break;
                default:
                    cell.SetCellValue("");
                    break;
            }
        }

        /// <summary>
        /// 设置Excel行
        /// </summary>
        private void SetExcelTitle(Dictionary<PropertyInfo, ExcelInfoAttribute> excelInfos)
        {
            IRow rowTitle = HssfSheet.CreateRow(0);
            int _cellIndex = 0;
            ICellStyle cellStyle = HssfWork.CreateCellStyle();
            cellStyle = StyleMessage.GetCellStyle(HssfWork, ExcelStyle.title);
            foreach (var item in excelInfos)
            {
                ICell celltitle = rowTitle.CreateCell(_cellIndex);
                celltitle.SetCellValue(item.Value.Name);
                celltitle.CellStyle = cellStyle;
                HssfSheet.SetColumnWidth(_cellIndex, item.Value.Width);
                _cellIndex++;
            }
        }

        /// <summary>
        /// 导出Excel操作
        /// </summary>
        private void DownLoadExcel()
        {
            string _path = TemporarySave();
            FileStream fileStream = new FileStream(_path, FileMode.Open);
            int fileContent = (int)fileStream.Length;
            byte[] byData = new byte[fileContent];
            fileStream.Read(byData, 0, fileContent);
            fileStream.Close();
            File.Delete(_path);
            DownLoadExcel(byData);
        }

        /// <summary>
        /// 临时保存
        /// </summary>
        /// <returns></returns>
        private string TemporarySave()
        {
            string _path = AppDomain.CurrentDomain.BaseDirectory;
            _path += string.Format(@"\TemporarySave{0}.xls", DateTime.Now.ToString("hhmmss"));
            using (FileStream file = new FileStream(_path, FileMode.Create))
            {
                HssfWork.Write(file);
                file.Close();
            }
            return _path;
        }

        /// <summary>
        /// 下载
        /// </summary>
        private void DownLoadExcel(byte[] byData)
        {
            HttpContext.Current.Response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}.xls", HttpUtility.UrlEncode(excelName)));
            HttpContext.Current.Response.AddHeader("Content-Transfer-Encoding", "binary");
            HttpContext.Current.Response.ContentType = "application/octet-stream";
            HttpContext.Current.Response.ContentEncoding = Encoding.GetEncoding("gb2312");
            HttpContext.Current.Response.BinaryWrite(byData);
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.Close();
        }

        /// <summary>
        /// 获取实体类的公共属性
        /// </summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <returns>实体类相应集合</returns>
        private Dictionary<PropertyInfo, ExcelInfoAttribute> GetPropInfo<T>()
        {
            Dictionary<PropertyInfo, ExcelInfoAttribute> _infos = new Dictionary<PropertyInfo, ExcelInfoAttribute>();
            Type _type = typeof(T);
            PropertyInfo[] _propInfos = _type.GetProperties();
            foreach (var propInfo in _propInfos)
            {
                object[] objAttrs = propInfo.GetCustomAttributes(typeof(ExcelInfoAttribute), true);
                if (objAttrs.Length > 0)
                {
                    ExcelInfoAttribute attr = objAttrs[0] as ExcelInfoAttribute;
                    if (attr != null)
                    {
                        _infos.Add(propInfo, attr);
                    }
                }
            }
            return _infos;
        }
    }
}