using System.Data;
using Oracle.ManagedDataAccess.Client; // ODP.NET Oracle managed provider 
using Oracle.ManagedDataAccess.Types; 
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

public class Program
{
	static void Main(string[] argv)
	{
		IniFile ini=new IniFile();
		ini.Load(@".\odict.ini");
		string dataSource=ini.GetKeyValue("database","datasource");
		string userId=ini.GetKeyValue("database","userid");
		string password=ini.GetKeyValue("database","password");
		string schema=ini.GetKeyValue("database","schema");
		string fs=ini.GetKeyValue("format","fontsize");
		
		if(schema==string.Empty)
		{
			schema=userId;
		}
		if(password==string.Empty)
		{
			password=Util.ReadPassword();
		}
		
		int fontSize;
		if(!Int32.TryParse(fs,out fontSize)) fontSize=11;
		
		string oradb = String.Format("Data Source={0};User Id={1};Password={2};",dataSource,userId,password); 
		OracleConnection conn = new OracleConnection(oradb);
		conn.Open(); 
		OracleCommand cmd = new OracleCommand(){BindByName=true,InitialLONGFetchSize=-1}; 
		cmd.Connection = conn; 
		//cmd.CommandText = "select * from all_tab_columns where owner=:owner order by table_name,column_id"; 
		cmd.CommandText=@"select  a.table_name,a.column_name,a.data_type,a.data_length,a.data_precision,a.data_scale,b.constraint_name,c.constraint_type,c.search_condition,c.r_constraint_name,c.index_name,d.table_name as r_table_name,d.column_name as r_column_name 
							from all_tab_columns a, (select * from all_cons_columns where owner=:owner) b,(select * from all_constraints where owner=:owner) c,all_cons_columns d 
							where a.owner=:owner and a.table_name=b.table_name(+) and a.column_name=b.column_name(+) 
							and b.constraint_name=c.constraint_name(+)
							and c.r_constraint_name=d.constraint_name(+)
							order by a.table_name,column_id";
		cmd.Parameters.Add("owner",schema);
		cmd.CommandType = CommandType.Text; 
		OracleDataReader dr = cmd.ExecuteReader(); 
		Tables tables=new Tables();
		string tableName="";
		string columnName="";
		int tableCount=0,columnCount=0;
		Table table=new Table(tableName);
		while(dr.Read())
		{		
			if(tableName!=dr.GetString(dr.GetOrdinal("table_name")))
			{
				tableName=dr.GetString(dr.GetOrdinal("table_name"));
				table=new Table(tableName);
				tables.Add(table);
				tableCount++;
				Console.WriteLine("Added table "+tableName);
				columnName="";
			}
			if(columnName!=dr.GetString(dr.GetOrdinal("column_name")))
			{
				columnName=dr.GetString(dr.GetOrdinal("column_name"));
				string dataType=dr.GetString(dr.GetOrdinal("data_type"));
				if(dataType=="NUMBER")
					dataType+="("+dr.GetValue(dr.GetOrdinal("data_precision")).ToString()+","+dr.GetValue(dr.GetOrdinal("data_scale")).ToString()+")";
				else if(dataType.StartsWith("TIMESTAMP"))
					dataType="TIMESTAMP";
				else 
					dataType+="("+dr.GetValue(dr.GetOrdinal("data_length")).ToString()+")";
				table.AddColumn(columnName,dataType);
				Console.WriteLine("......Added Column "+columnName);
				columnCount++;
			}
			if(dr.GetValue(dr.GetOrdinal("constraint_type")).ToString()=="") continue; 
			if(table.LastColumn.constraints!="") table.LastColumn.constraints+="\n";
			switch(dr.GetString(dr.GetOrdinal("constraint_type")))
			{
				case "C":
					string check=dr.GetString(dr.GetOrdinal("search_condition"));
					if(check.EndsWith("IS NOT NULL")) table.LastColumn.constraints+="Not Null";
					else table.LastColumn.constraints+=check;
					break;
				case "R":
					table.LastColumn.constraints+="Foreign Key: "+dr.GetString(dr.GetOrdinal("r_table_name"))+"."+dr.GetString(dr.GetOrdinal("r_column_name"));
					table.LastColumn.foreignKey=dr.GetString(dr.GetOrdinal("r_table_name"))+"."+dr.GetString(dr.GetOrdinal("r_column_name"));
					break;
				case "P":
					table.LastColumn.constraints+="Primary Key";
					break;
				case "U":
					table.LastColumn.constraints+="Unique";
					break;
				default:
					table.LastColumn.constraints+=dr.GetString(dr.GetOrdinal("constraint_type"));
					break;
			}
		}
		conn.Dispose();
		foreach(Table tab in tables)
		{
			foreach(Column col in tab.columns)
			{
				if(col.foreignKey=="") continue;
				string tname=col.foreignKey.Split(new char[]{'.'})[0];
				string cname=col.foreignKey.Split(new char[]{'.'})[1];
				Column c=tables.GetTableByName(tname).GetColumnByName(cname);
				if(c.referencedBy!="") c.referencedBy+="\r\n";
				c.referencedBy+=tab.name+"."+col.name;
			}
		}
		Console.WriteLine("Table number: "+tableCount.ToString()+" Column number: "+columnCount.ToString());
		Console.Write("Writing file...");
		Output(schema,tables);
		Console.WriteLine("Done.");
	}
	static void Output(string schema,List<Table>tables)
	{
		IniFile ini=new IniFile();
		ini.Load(@".\odict.ini");
		int fontSize=ini.GetInt("format","fontsize",11);
		using(Novacode.DocX doc=Novacode.DocX.Create(schema+".docx"))
		{
			doc.PageWidth=ini.GetInt("format","pagewidth",816);
			doc.PageHeight=ini.GetInt("format","pageheight",1056);
			doc.MarginTop=doc.MarginBottom=doc.MarginLeft=doc.MarginRight=ini.GetInt("format","pagemargin",48);
			doc.InsertParagraph("Columns: Field Name | Data Type | Constraints | Referenced by | Description \n").FontSize(fontSize);
			int cols=5;
			foreach(Table table in tables)
			{
				Novacode.Table t=doc.AddTable(table.columns.Count+2,cols);
				t.Rows[0].MergeCells(0,cols-1);
				for(int i=0;i<cols-1;i++)
					t.Rows[0].Cells[0].RemoveParagraphAt(0);
				t.Rows[0].Cells[0].Paragraphs[0].Append(table.name);
				t.Rows[0].Cells[0].Paragraphs[0].Bold().FontSize(fontSize).Alignment=Novacode.Alignment.center;
				t.Rows[0].Cells[0].FillColor=Color.Silver;
				int row=1;
				foreach(Column col in table.columns)
				{
					t.Rows[row].Cells[0].Paragraphs[0].Append(col.name).FontSize(fontSize);
					t.Rows[row].Cells[1].Paragraphs[0].Append(col.dataType).FontSize(fontSize);
					t.Rows[row].Cells[2].Paragraphs[0].Append(col.constraints).FontSize(fontSize);
					t.Rows[row++].Cells[3].Paragraphs[0].Append(col.referencedBy).FontSize(fontSize);
				}
				t.Rows[row].MergeCells(0,4);
				for(int i=0;i<cols-1;i++)
					t.Rows[row].Cells[0].RemoveParagraphAt(0);
				t.Rows[row].Cells[0].Paragraphs[0].Append("Note:").FontSize(fontSize);
				doc.InsertTable(t);
				doc.InsertParagraph("");
			}
			doc.Save();
		}
	}
}