# Gini

Gini is an [INI file format](http://en.wikipedia.org/wiki/INI_file) parser written in C#. It is a distributed as a [NuGet package](http://www.nuget.org/packages/Gini.Source/) that adds a single C# source file to a C# project.

## Usage

The most basic method in Gini is called `Parse` that parses the INI file format and yields [groups](http://msdn.microsoft.com/en-us/library/vstudio/bb344977.aspx) of [key-value pairs](http://msdn.microsoft.com/en-us/library/5tbh8a42.aspx). So sections in an INI file format become groups where the group key is the section name and the entries of the section become key-value pairs of the group.

The following code C# example:

    const string ini = @"
        ; last modified 1 April 2001 by John Doe
        [owner]
        name=John Doe
        organization=Acme Widgets Inc.
    
        [database]
        ; use IP address in case network name resolution is not working
        server=192.0.2.62     
        port=143
        file=payroll.dat";

    foreach (var g in Ini.Parse(ini))
    {
        Console.WriteLine("[{0}]", g.Key);
        foreach (var e in g)
            Console.WriteLine("{0}={1}", e.Key, e.Value);
    }

produces the output:

    [owner]
    name=John Doe
    organization=Acme Widgets Inc.
    [database]
    server=192.0.2.62
    port=143
    file=payroll.dat

The `ParseHash` method parses the INI file format and return a dictionary of sections where each section is itself a dictionary of entries:

    const string ini = @"
        ; last modified 1 April 2001 by John Doe
        [owner]
        name=John Doe
        organization=Acme Widgets Inc.
    
        [database]
        ; use IP address in case network name resolution is not working
        server=192.0.2.62     
        port=143
        file=payroll.dat";

    var config = Ini.ParseHash(ini);
    var owner = config["owner"];
    Console.WriteLine("Owner Name = {0}", owner["name"]);
    Console.WriteLine("Owner Organization = {0}", owner["organization"]);
    var database = config["database"];
    Console.WriteLine("Database Server = {0}", database["server"]);
    Console.WriteLine("Database Port = {0}", database["port"]);
    Console.WriteLine("Database File = {0}", database["file"]);

The output produced by the preceding code is:

    Owner Name = John Doe
    Owner Organization = Acme Widgets Inc.
    Database Server = 192.0.2.62
    Database Port = 143
    Database File = payroll.dat

The `ParseHashFlat` method is like `ParseFlat` except it returns a single dictionary of entries. The section names are merged with the key names via a mapper function to generate unique entires:

    const string ini = @"
        ; last modified 1 April 2001 by John Doe
        [owner]
        name=John Doe
        organization=Acme Widgets Inc.
    
        [database]
        ; use IP address in case network name resolution is not working
        server=192.0.2.62     
        port=143
        file=payroll.dat";

    foreach (var e in Ini.ParseFlatHash(ini, (s, k) => s + "." + k))
        Console.WriteLine("{0} = {1}", e.Key, e.Value);

The output is as follows:

    owner.name = John Doe
    owner.organization = Acme Widgets Inc.
    database.server = 192.0.2.62
    database.port = 143
    database.file = payroll.dat

If Gini is compiled with the `GINI_DYNAMIC` conditional compilation symbol defined then it adds the `ParseObject` method that parses the INI file format into a dynamic object:

    const string ini = @"
        ; last modified 1 April 2001 by John Doe
        [owner]
        name=John Doe
        organization=Acme Widgets Inc.
    
        [database]
        ; use IP address in case network name resolution is not working
        server=192.0.2.62     
        port=143
        file=payroll.dat";

    var config = Ini.ParseObject(ini);
    var owner = config.Owner;
    Console.WriteLine("Owner Name = {0}", owner.Name);
    Console.WriteLine("Owner Organization = {0}", owner.Organization);
    var database = config.Database;
    Console.WriteLine("Database Server = {0}", database.Server);
    Console.WriteLine("Database Port = {0}", database.Port);
    Console.WriteLine("Database File = {0}", database.File);

The output is:

    Owner Name = John Doe
    Owner Organization = Acme Widgets Inc.
    Database Server = 192.0.2.62
    Database Port = 143
    Database File = payroll.dat

Note that the lookup of properties on the dynamic object is case-insensitive.

Like there is `ParseFlatHash` for `ParseFlat`, there is `ParseFlatObject` for `ParseObject` that returns a single object of entries with a mapper function determinig how to merge section and key names:

    const string ini = @"
        ; last modified 1 April 2001 by John Doe
        [owner]
        name=John Doe
        organization=Acme Widgets Inc.
    
        [database]
        ; use IP address in case network name resolution is not working
        server=192.0.2.62     
        port=143
        file=payroll.dat";

    var config = Ini.ParseFlatObject(ini, (s, k) => s + k);
    Console.WriteLine("Owner Name = {0}", config.OwnerName);
    Console.WriteLine("Owner Organization = {0}", config.OwnerOrganization);
    Console.WriteLine("Database Server = {0}", config.DatabaseServer);
    Console.WriteLine("Database Port = {0}", config.DatabasePort);
    Console.WriteLine("Database File = {0}", config.DatabaseFile);

The output is again:

    Owner Name = John Doe
    Owner Organization = Acme Widgets Inc.
    Database Server = 192.0.2.62
    Database Port = 143
    Database File = payroll.dat

Syntax errors in the INI file format are silently ignored. Only bits that can be successfully parsed are returned or processed.
