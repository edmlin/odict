using System.Collections.Generic;
class Column
{
	public string name;
	public string dataType;
	public string constraints="";
	public string foreignKey="";
	public string referencedBy="";
}

class Tables:List<Table>
{
	public Table GetTableByName(string name)
	{
		foreach(Table tab in this)
		{
			if(tab.name==name) return tab;
		}
		return null;
	}
}

class Table
{
	public string name;
	public List<Column> columns;
	public Table(string tableName)
	{
		name=tableName;
		columns=new List<Column>();
	}
	public void AddColumn(string columnName,string columnType)
	{
		columns.Add(new Column(){name=columnName,dataType=columnType});
	}
	public Column LastColumn
	{
		get{return columns[columns.Count-1];}
	}
	public Column GetColumnByName(string name)
	{
		foreach(Column col in columns)
		{
			if(col.name==name) return col;
		}
		return null;
	}
}