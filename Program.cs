// See https://aka.ms/new-console-template for more information

Console.WriteLine("Text to Object Converter!");

var data = "m1,    c1,     req1,   act1,   not started, 11, task 1.1;" +
            "m1,    c1,     req1,   act2,   not started, 21, task 2.1;" +
            "m1,    c1,     req1,   act2,   not started, 22, task 2.2;" +
            "m1,    c1,     req1,   act3,   in progress, 31, task 3.1;" +
            "m2,    c2,     req2,   act21,  not started, 41, task 4.1";


Console.WriteLine("Input Data:");
Console.WriteLine(data);

var p1 = new text2object.Parser();
p1.InitData(data);
p1.Convert();
