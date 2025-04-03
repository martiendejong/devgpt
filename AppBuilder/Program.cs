// See https://aka.ms/new-console-template for more information
using DevGPT.NewAPI;
using OpenAI.Chat;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

string openAiApiKey = "***REMOVED***";

// Vera frontend
string appDir = @"C:\Projects\socialmediahulp\frontend";
string buildFile = "runquasar.bat";
string documentStoreRoot = @"C:\Projects\socialmediahulp\frontend\src";
string embeddingsFile = @"C:\Projects\socialmediahulp\frontend\embeddings";
string logFilePath = @"C:\Projects\socialmediahulp\frontend\log";

var appFolderStoreConfig = new DocumentStoreConfig(documentStoreRoot, embeddingsFile, openAiApiKey);
var store = new DocumentStore(appFolderStoreConfig);

//var remove = store.Embeddings.Where(e => e.Path.Contains("LocationStore")).ToList();
//foreach(var item in remove)
//{
//    store.Embeddings.Remove(item);
//}
//var dir = new DirectoryInfo(@"C:\projects\beheerportaal\quasar\src");
//var files = dir.GetFiles("LocationStore.ts", SearchOption.AllDirectories);

// todo only if store empty (first time)
//var dir = new DirectoryInfo(documentStoreRoot);
//var files = dir.GetFiles("*.js", SearchOption.AllDirectories)
//    .Concat(dir.GetFiles("*.ts", SearchOption.AllDirectories))
//    .Concat(dir.GetFiles("*.vue", SearchOption.AllDirectories))
//    .ToList();
//foreach (var file in files)
//{
//    var relPath = file.FullName.Substring((documentStoreRoot + "\\").Length);
//    await store.AddDocument(file.FullName, file.Name, relPath, false);
//}

await store.UpdateEmbeddings();
store.SaveEmbeddings();
var prompt =
@"You are a quasar developer working on the Vera AI App. Your goal is to write quasar code as clean as possible. You are an expert at SONAR, SOLID, design patterns, coding best practices and you apply them when possible. Maintain a task list in the file tasks.txt";

//.htaccess - protects the whole folder with a password
var promptMessages = new List<ChatMessage>() { new SystemChatMessage(prompt) };


var generator = new DocumentGenerator(store, promptMessages, appFolderStoreConfig.OpenAiApiKey, logFilePath);
//await generator.UpdateStore("Add a BrUserHub that works like the BuildingHub but for BrUsers. The BrUserHub should later be integrated in the BrUserStore as a separate task");



static Tuple<string, string> GetQuasarBuildOutput(string workingDirectory, string file)
{
    ProcessStartInfo psi = new ProcessStartInfo
    {
        WorkingDirectory = workingDirectory,
        FileName = workingDirectory + "\\" + file,
        //Arguments = "build",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };

    using (Process process = new Process { StartInfo = psi })
    {
        process.Start();

        // Read output and errors
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        //var output = "";
        //var error = "";

        process.WaitForExit();

        Console.WriteLine("Build Output:\n" + output);
        Console.WriteLine("Build Errors:\n" + error);
        var index = output.IndexOf("src/components/");
        if (index == -1)
            output = "";
        else
            output = output.Substring(index);

        return new Tuple<string, string>(output, error);
    }
}


var message = @"Based on this request create new tasks and add them to the tasks list: Create a user management module where a list of users can be viewed, with an edit and a delete icon. 
the delete icon should show a confirmation dialog before deleting the item. the edit icon should open an inline editor where the user info can be modified and with a save button stored again.
everything should be in dutch. it should be in line with the design of the other pages. new pages should be added to the router and to the menu in the left drawer.";
await generator.UpdateStore(message);
//var build = GetQuasarBuildOutput(appDir, buildFile);
//var output = build.Item1;
//message = @$"This is the build result. If there are any errors solve the first one: {output}. If there are no errors contineu.";
//await generator.UpdateStore(message);

for (var j = 0; j < 10; ++j)
{
    for (var i = 0; i < 3; ++i)
    {
        await generator.UpdateStore("pick the first task from tasks.txt that is not marked done, execute it and mark it as done");
    }
    await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");
}

//build = GetQuasarBuildOutput(appDir, buildFile);
//output = build.Item1;
//message = @$"This is the build result. If there are any errors solve the first one: {output}. If there are no errors contineu.";
//await generator.UpdateStore(message);




//await generator.UpdateStore("Add a OwningCompanyHub that works like the BuildingHub but for OwningCompanies. The OwningCompanyHub should later be integrated in the OwningCompanyStore as a separate task");
//await generator.UpdateStore("Add a InstallationCompanyHub that works like the BuildingHub but for InstallationCompanies. The InstallationCompanyHub should later be integrated in the InstallationCompanyStore as a separate task");


//store.SaveEmbeddings();

//await generator.UpdateStore("create deposit.php");
//await generator.UpdateStore("create withdraw.php");
//await generator.UpdateStore("create investors.php");
//await generator.UpdateStore("create purchase.php");
//await generator.UpdateStore("create sell.php");
//await generator.UpdateStore("create expense.php");
//await generator.UpdateStore("create .htaccess");
//await generator.UpdateStore("Verify that the application is created correctly and update files where neccessary");
//await generator.UpdateStore("create caretakers.php");
//await generator.UpdateStore("create addcaretaker.php");
//await generator.UpdateStore("create company.php");

//await generator.UpdateStore("make sure deposit.php has the right menu and mobile layout");
//await generator.UpdateStore("make sure withdraw.php has the right menu and mobile layout");
//await generator.UpdateStore("make sure investors.php has the right menu and mobile layout");
//await generator.UpdateStore("make sure purchase.php has the right menu and mobile layout");
//await generator.UpdateStore("make sure sell.php has the right menu and mobile layout");
//await generator.UpdateStore("make sure expense.php has the right menu and mobile layout");
//await generator.UpdateStore("make sure caretakers.php has the right menu and mobile layout");
//await generator.UpdateStore("make sure addcaretaker.php has the right menu and mobile layout");
//await generator.UpdateStore("make sure company.php has the right menu and mobile layout");
//await generator.UpdateStore("create a tasks.txt. the first task: verify that all pages have a similar layout and for each page that doesnt have this layout create a task to update the layout for that page");
//await generator.UpdateStore("add a task: verify that all pages have a mobile friendly layout and for each page that doesnt have this layout create a task to update the layout for that page");
//await generator.UpdateStore("add a task: verify that all pages have the right menu items and for each page that doesnt have the right menu items create a task to update the menu for that page");
//await generator.UpdateStore("add a task: verify that all form input where an item is picked, like an animal by id, investor or caretaker or other items that are in a list that this input is a dropdown. for each page where this is not the case create a task to make it a dropdown");
//await generator.UpdateStore("add a task: verify that no pages have errors when running them and for each page that does have errors create a task to update the code for that page to make sure no errors exist");
//await generator.UpdateStore("verify if there is anything that still needs to be done and if so make tasks for that");

//for (var j = 0; j < 10; ++j)
//{
//    for (var i = 0; i < 3; ++i)
//    {
//        await generator.UpdateStore("pick the first task from tasks.txt that is not marked done, execute it and mark it as done");
//    }
//    await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");
//}

//await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");
//await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");

//await generator.UpdateStore("verify if there is anything that still needs to be done and if so make tasks for that");

//for (var i = 0; i < 3; ++i)
//{
//    await generator.UpdateStore("pick the first task from tasks.txt that is not marked done, execute it and mark it as done");
//}
//await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");
//await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");
