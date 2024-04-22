using System.Linq;
using System;
using System.Dynamic;
using System.Collections.Generic;

namespace text2object;

public class Parser
{

    static List<string> objNames;
    static Dictionary<string, List<string>> objDefs;
    static Dictionary<string, string> objRels;
    static Dictionary<string, List<ExpandoObject>> objList;

    static string inputText;
    static Dictionary<int, string> inputRows;

    static List<ExpandoObject> outputObject;

    public void InitData(string data)
    {
        inputText = data;
        while (inputText.Contains(" "))
            inputText = inputText.Replace(" ", string.Empty);

        inputRows = inputText.Split(";").ToList().Select((s, i) => new { s, i }).ToDictionary(x => x.i, x => x.s);

        objNames = new List<string>()
            {
                AppConstant.REQUIREMENT_OBJ,
                AppConstant.ACTIVTY_OBJ,
                AppConstant.TASK_OBJ
            };

        // stored as parent, child strings
        objRels = new Dictionary<string, string>
            {
                { string.Empty, AppConstant.REQUIREMENT_OBJ },
                { AppConstant.REQUIREMENT_OBJ, AppConstant.ACTIVTY_OBJ },
                { AppConstant.ACTIVTY_OBJ, AppConstant.TASK_OBJ }
            };

        objDefs = new Dictionary<string, List<string>>();
        //Requirement
        var fldList = new List<string>() { "__rowid", "module", "component", "title" };
        objDefs.Add(AppConstant.REQUIREMENT_OBJ, fldList);
        //Activity
        fldList = new List<string>() { "__rowid", "act_name", "act_status" };
        objDefs.Add(AppConstant.ACTIVTY_OBJ, fldList);
        //Task
        fldList = new List<string>() { "__rowid", "t_id", "t_desc" };
        objDefs.Add(AppConstant.TASK_OBJ, fldList);


        objList = new Dictionary<string, List<ExpandoObject>>();
        foreach (var r in objNames)
        {
            objList.Add(r, new List<ExpandoObject>());

        }

    }
    public void Convert()
    {
        outputObject = new List<ExpandoObject>();

        //Initialize the object definitions and the input data;
        //InitData();

        //Read the input data and create the objects list            
        foreach (var line in inputRows)
        {
            Console.WriteLine(line.Key + "==>" + line.Value);

            foreach (var od in objDefs)
            {
                var oName = od.Key;
                var oDef = od.Value;
                var oVals = GetValues(oName, line.Key, line.Value);
                dynamic o = GetObject(oDef, oVals);

                List<ExpandoObject> oList = objList[oName];
                CheckAndAddObject(oList, o);
                objList[oName] = oList;

            }



        }

        var rootObjectNames = new List<string>();
        var leafObjectNames = new List<string>();

        //Find the leaf objects and root objects
        foreach (var o1 in objNames)
        {
            var isRootObj = false;
            var isLeafObj = true;

            foreach (var rel in objRels)
            {
                if ((o1 == rel.Value) && (rel.Key == string.Empty))
                    isRootObj = true;

                if (o1 == rel.Key)
                    isLeafObj = false;

            }

            if (isRootObj)
                rootObjectNames.Add(o1);

            if (isLeafObj)
                leafObjectNames.Add(o1);

        }

        //append child objects to parent
        foreach (var leaf in leafObjectNames)
        {
            var leafObjects = objList[leaf];

            var parentObjectName = objRels.Where(x => x.Value == leaf).First().Key;
            var parentObjects = objList[parentObjectName];

            var childParentGroup = new Dictionary<int, List<ExpandoObject>>();

            foreach (dynamic c in parentObjects)
                childParentGroup.Add(c.__rowId, new List<ExpandoObject>());

            foreach (var o in leafObjects)
            {
                var leafRowId = o.Where(x => x.Key == "__rowid").First().Value.ToString();
                Console.Write(leafRowId);

                var rowId = int.Parse(leafRowId);

                dynamic p1 = GetObject(objDefs[parentObjectName], GetValues(parentObjectName, rowId, inputRows[rowId]));
                dynamic po = null;

                var parentFound = false;
                foreach (var pob in parentObjects)
                {
                    parentFound = AreExpandosEquals(p1, pob);

                    if (parentFound)
                    {
                        po = pob;
                        break;
                    }

                }

                if (parentFound)
                {
                    Console.WriteLine("-->" + po.__rowid);
                    int rwId = int.Parse(po.__rowid);

                    var existingChildren = childParentGroup[rwId];
                    existingChildren.Add(o);
                    childParentGroup[rwId] = existingChildren;
                }
                else
                {
                    Console.WriteLine("....failed to match a parent!");
                    throw new Exception("parent not found");
                }

                var parentObj = parentObjects.Cast<dynamic>().Where((dynamic x) => x.__rowid == leafRowId);

                /* if (parentObj.Any())
                {
                    IDictionary<string, object> pok = parentObj.First();                         
                    pok[leaf + "List"] = o;

                }
                else
                {

                } */

            }

            foreach (var o in objNames)
            {
                if (o == leaf)
                {

                }

            }
        }

        /* Console.WriteLine(new string('_', 25));
        Console.WriteLine("BEGIN OUTPUT");

        Console.WriteLine("Root Objects");
        Console.WriteLine(new string('-', 25));
        rootObjectNames.ForEach(x => Console.WriteLine(x));
        Console.WriteLine(new string('~', 25));

        Console.WriteLine("Leaf Objects");
        Console.WriteLine(new string('-', 25));
        leafObjectNames.ForEach(x => Console.WriteLine(x));
        Console.WriteLine(new string('~', 25));

        Console.WriteLine("Objects List");
        foreach (var opObj in objList)
        {
            Console.WriteLine(opObj.Key);
            Console.WriteLine(new string('-', 25));
            Console.WriteLine(JsonConvert.SerializeObject(opObj.Value, Formatting.Indented));
            Console.WriteLine(new string('~', 25));
        }
*/
        Console.WriteLine("END OUTPUT");
        Console.WriteLine(new string('_', 25));
    }

    private static void CheckAndAddObject(List<ExpandoObject> objList, ExpandoObject newObj)
    {
        var objFound = false;

        foreach (var o in objList)
        {
            objFound = AreExpandosEquals(o, newObj);
            if (objFound)
                break;
        }

        if (!objFound)
            objList.Add(newObj);

    }

    public static bool AreExpandosEquals(ExpandoObject obj1, ExpandoObject obj2)
    {
        var obj1AsColl = (ICollection<KeyValuePair<string, object>>)obj1;
        var obj2AsDict = (IDictionary<string, object>)obj2;

        // Make sure they have the same number of properties
        if (obj1AsColl.Count != obj2AsDict.Count)
            return false;

        foreach (var pair in obj1AsColl.Where(x => x.Key != "__rowid"))
        {
            // Try to get the same-named property from obj2
            object o;
            if (!obj2AsDict.TryGetValue(pair.Key, out o))
                return false;

            // Property names match, what about the values they store?
            if (!object.Equals(o, pair.Value))
                return false;
        }

        // Everything matches
        return true;
    }

    private static dynamic GetObject(List<string> props, List<string> values = null)
    {
        IDictionary<string, object> obj = new ExpandoObject();
        int i = -1;
        foreach (var p in props)
        {
            i++;
            if (null == values || values[i] == null)
                obj[p] = string.Empty;
            else
                obj[p] = values[i];
        }
        return obj;
    }

    private static List<string> GetValues(string objName, int lineNo, string lineData)
    {
        var rowData = lineData.Split(",");
        var objValues = new List<string>();

        switch (objName)
        {
            case AppConstant.REQUIREMENT_OBJ:
                {
                    objValues.Add(lineNo.ToString());
                    objValues.Add(rowData[1]);
                    objValues.Add(rowData[2]);
                    objValues.Add(rowData[3]);

                    break;
                }

            case AppConstant.ACTIVTY_OBJ:
                {
                    objValues.Add(lineNo.ToString());
                    objValues.Add(rowData[3]);
                    objValues.Add(rowData[4]);
                    break;
                }

            case AppConstant.TASK_OBJ:
                {
                    objValues.Add(lineNo.ToString());
                    objValues.Add(rowData[5]);
                    objValues.Add(rowData[6]);
                    break;
                }

        }

        return objValues;
    }

}
