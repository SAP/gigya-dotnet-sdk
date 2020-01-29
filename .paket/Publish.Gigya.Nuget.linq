<Query Kind="Program">
  <NuGetReference>Gigya.Build.Solo</NuGetReference>
  <NuGetReference>Semver</NuGetReference>
  <Namespace>Gigya.Build.Solo</Namespace>
  <Namespace>Gigya.Build.Solo.Command</Namespace>
  <Namespace>Semver</Namespace>
</Query>

void Main()
{
    // Author: A.Chirlin
    // 1.0.0 - 29/01/2020
    
    // The following will be performed
    
    
    // Increase version in 
    //   1) Gigya.Socialize.SDK\Properties\AssemblyInfo.cs
    //   2) Gigya.Socialize.SDK\GSRequest.cs

    var autoVersionIncrease = true;

    var qPath = new FileInfo(LINQPad.Util.CurrentQuery.FilePath).Directory.FullName;
    var root =   Path.Combine(qPath, @"..\");
    var prRoot = Path.Combine(qPath, @"..\Gigya.Socialize.SDK");

    var solo = new Solution("Gigya.Socialize.SDK",root);

    solo.Info.SolutionVersion.Dump("Current version");

    var from = solo.Info.SolutionVersion;
    var v = from.Split(new[] { '.' });
    var to3 = $"{v[0]}.{v[1]}.{int.Parse(v[2])+1}";
    var to = $"{to3}.0";

    if (autoVersionIncrease)
    {
        var template = (Path.Combine(prRoot, "paket.template"));
        
        fr(template, @"\d+\.\d+\.\d+", @to3);
        
        fr( Path.Combine(prRoot, "GSRequest.cs"), @"public const String version = ""\d+\.\d+\.\d+"";", $@"public const String version = ""{to3}"";");
        
        solo.UpdateVersion(@to);
    }

    to.Dump("Publishing version");
    
    solo.Settings.BuildConfiguration = "Release";
    solo.Settings.OpenNotepadOnFailure = true;

    solo.Build.OnComplete += (object sender, DevEnvComplete args) => {
        if(!args.IsFailed){
        
            "Build succeeded".Dump();
            
            solo.Paket.EnsurePaketExe(true);
           
            var package = Path.Combine(root, $"GSCSharpSDK.{to3}.nupkg");

            var pack = new Executable(Path.Combine(root, ".paket", "paket.exe"));
            pack.WorkingDirectory = root;

            pack.Arguments = $"pack --build-config {solo.Settings.BuildConfiguration} --template {prRoot}\\paket.template --version {to3} {root}";
            
            var logFile = Path.GetTempFileName();
            pack.StandardOutputFileName = logFile;
            
            Console.WriteLine("Packing into: " + package);
            
            var packResult = pack.Run().Dump("Packing result (should be 0)");
            Console.WriteLine(File.ReadAllText(logFile));
            
            if(File.Exists(package) && packResult == 0)
            {
                var push = new Executable(Path.Combine(root, ".nuget", "NuGet.exe"));
                pack.WorkingDirectory = root;
                push.Arguments = $"push -Source http://nuget.gigya.net/nugetForVS/nuget/ {package}";
                push.StandardOutputFileName = Path.GetTempFileName();
                var pushResult = push.Run().Dump($"Pushed (should be 0): {package}");
                if (pushResult != 0)
                    Console.WriteLine($"Failed to push the package: {package}, Details: " + File.ReadAllText(push.StandardOutputFileName));
            }
            else
            {
                Console.Error.Write("Failed to pack, package not found: " + package);
            }
        }
    };
    
    solo.Build.Build();
}

void fr(string filename, string pattern, string replacement)
{
    File.WriteAllText(filename, Regex.Replace(File.ReadAllText(filename), pattern, replacement));
}