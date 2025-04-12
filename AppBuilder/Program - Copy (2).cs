//// See https://aka.ms/new-console-template for more information
//using DevGPT.NewAPI;
//using OpenAI.Chat;
//using System.ComponentModel.DataAnnotations;
//using System.Diagnostics;


//var appFolderStoreConfig = new DocumentStoreConfig(@"C:\projects\beheerportaal\quasar\src", @"C:\projects\beheerportaal\quasar\embeddings", "");
//var store = new DocumentStore(appFolderStoreConfig);

////var remove = store.Embeddings.Where(e => e.Path.Contains("LocationStore")).ToList();
////foreach(var item in remove)
////{
////    store.Embeddings.Remove(item);
////}
////var dir = new DirectoryInfo(@"C:\projects\beheerportaal\quasar\src");
////var files = dir.GetFiles("LocationStore.ts", SearchOption.AllDirectories);


////var dir = new DirectoryInfo(@"C:\projects\beheerportaal\quasar\src");
////var files = dir.GetFiles("*.js", SearchOption.AllDirectories)
////    .Concat(dir.GetFiles("*.ts", SearchOption.AllDirectories))
////    .Concat(dir.GetFiles("*.vue", SearchOption.AllDirectories))
////    .ToList();
////foreach (var file in files)
////{
////    var relPath = file.FullName.Substring(@"C:\projects\beheerportaal\quasar\src\".Length);
////    await store.AddDocument(file.FullName, file.Name, relPath, false);
////}

//await store.UpdateEmbeddings();
//store.SaveEmbeddings();
//var prompt =
//@"You are a quasar developer working on the brcontrols beheerportaal frontend.";

////.htaccess - protects the whole folder with a password
//var promptMessages = new List<ChatMessage>() { new SystemChatMessage(prompt) };
//var generator = new DocumentGenerator(store, promptMessages, appFolderStoreConfig.OpenAiApiKey, @"C:\projects\beheerportaal\quasar\log");
////await generator.UpdateStore("Add a BrUserHub that works like the BuildingHub but for BrUsers. The BrUserHub should later be integrated in the BrUserStore as a separate task");

//var fix = @" ERROR(vue-tsc)  Property 'fetchSelectableInstallationCompanies' does not exist on type 'Store<""installationCompany"", { installationCompanies: InstallationCompany[]; isLoading: boolean; initialized: boolean; }, { getInstallationCompany: (state: { installationCompanies: { ...; }[]; isLoading: boolean; initialized: boolean; } & PiniaCustomStateProperties<...>) => (id: string) => InstallationCompany | unde...'. Did you mean 'fetchInstallationCompanies'?
// FILE  C:/projects/beheerportaal/quasar/src/components/admin/ChooseBuildingContractor.vue:85:15

//    83 |
//    84 | onMounted(async () => {
//  > 85 |   await store.fetchSelectableInstallationCompanies();
//       |               ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    86 |   availableItems.value = store.installationCompanies;
//    87 |
//    88 |   var item = props.item;

// ERROR(vue-tsc)  Property 'fetchSelectableInstallationCompanies' does not exist on type 'Store<""installationCompany"", { installationCompanies: InstallationCompany[]; isLoading: boolean; initialized: boolean; }, { getInstallationCompany: (state: { installationCompanies: { ...; }[]; isLoading: boolean; initialized: boolean; } & PiniaCustomStateProperties<...>) => (id: string) => InstallationCompany | unde...'. Did you mean 'fetchInstallationCompanies'?
// FILE  C:/projects/beheerportaal/quasar/src/components/admin/ShowBuildingContractor.vue:31:15

//    29 |
//    30 | onMounted(async () => {
//  > 31 |   await store.fetchSelectableInstallationCompanies();
//       |               ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    32 |   availableItems.value = store.installationCompanies;
//    33 |
//    34 |   var item = props.item;

// ERROR(vue-tsc)  Property 'fetchSelectableInstallationCompanies' does not exist on type 'Store<""installationCompany"", { installationCompanies: InstallationCompany[]; isLoading: boolean; initialized: boolean; }, { getInstallationCompany: (state: { installationCompanies: { ...; }[]; isLoading: boolean; initialized: boolean; } & PiniaCustomStateProperties<...>) => (id: string) => InstallationCompany | unde...'. Did you mean 'fetchInstallationCompanies'?
// FILE  C:/projects/beheerportaal/quasar/src/components/admin/ShowBuildingContractors.vue:29:15

//    27 |
//    28 | onMounted(async () => {
//  > 29 |   await store.fetchSelectableInstallationCompanies();
//       |               ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    30 |   availableItems.value = store.installationCompanies;
//    31 |
//    32 |   var buildingId = props.building?.id ?? '';

// ERROR(vue-tsc)  Property 'fetchBrUsersForOrganization' does not exist on type 'Store<""brUser"", { currentUser: BrUser | undefined; brUsers: BrUser[]; isLoading: boolean; initialized: boolean; }, { getBrUser: (state: { currentUser: { [x: string]: unknown; userName?: string | undefined; ... 12 more ...; description?: string | undefined; } | undefined; brUsers: { ...; }[]; isLoading: boolean; init...'.
// FILE  C:/projects/beheerportaal/quasar/src/components/relations/BuildingUserManagement.vue:209:21

//    207 |   availableApplications.value = applicationStore.applications;
//    208 |
//  > 209 |   await brUserStore.fetchBrUsersForOrganization();
//        |                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    210 |   availableUsers.value = brUserStore.brUsers;
//    211 |
//    212 |   await loadBuildingUsers();

// ERROR(vue-tsc)  Property 'fetchSelectableInstallationCompanies' does not exist on type 'Store<""installationCompany"", { installationCompanies: InstallationCompany[]; isLoading: boolean; initialized: boolean; }, { getInstallationCompany: (state: { installationCompanies: { ...; }[]; isLoading: boolean; initialized: boolean; } & PiniaCustomStateProperties<...>) => (id: string) => InstallationCompany | unde...'. Did you mean 'fetchInstallationCompanies'?
// FILE  C:/projects/beheerportaal/quasar/src/components/relations/OwnerContractorsManagement.vue:100:25

//     98 |
//     99 |   // Fetch available buildings from the store
//  > 100 |   await contractorStore.fetchSelectableInstallationCompanies(); // Assuming there's a method to fetch all buildings
//        |                         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    101 |   availableItems.value = contractorStore.installationCompanies; // Assuming `getAllBuildings` returns the list of buildings
//    102 | });
//    103 |

// ERROR(vue-tsc)  Property 'fetchBrUserWithOrganizations' does not exist on type 'Store<""brUser"", { currentUser: BrUser | undefined; brUsers: BrUser[]; isLoading: boolean; initialized: boolean; }, { getBrUser: (state: { currentUser: { [x: string]: unknown; userName?: string | undefined; ... 12 more ...; description?: string | undefined; } | undefined; brUsers: { ...; }[]; isLoading: boolean; init...'.
// FILE  C:/projects/beheerportaal/quasar/src/components/relations/permissions/UserOrganizationPermissionsManagement.vue:45:21

//    43 |
//    44 | onMounted(async () => {
//  > 45 |   await brUserStore.fetchBrUserWithOrganizations(userId);
//       |                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    46 |   await organizationStore.fetchOrganization(orgId);
//    47 |   if(organization.value != undefined)
//    48 |     editPermissions(organization.value);

// ERROR(vue-tsc)  Property 'fetchBrUserWithBuildings' does not exist on type 'Store<""brUser"", { currentUser: BrUser | undefined; brUsers: BrUser[]; isLoading: boolean; initialized: boolean; }, { getBrUser: (state: { currentUser: { [x: string]: unknown; userName?: string | undefined; ... 12 more ...; description?: string | undefined; } | undefined; brUsers: { ...; }[]; isLoading: boolean; init...'.
// FILE  C:/projects/beheerportaal/quasar/src/components/relations/UserBuildingManagement.vue:153:15

//    151 |
//    152 | onMounted(async () => {
//  > 153 |   brUserStore.fetchBrUserWithBuildings(userId);
//        |               ^^^^^^^^^^^^^^^^^^^^^^^^
//    154 |
//    155 |   await buildingStore.fetchUserBuildings(userId);
//    156 |   availableBuildings.value = buildingStore.buildings;

// ERROR(vue-tsc)  Property 'fetchBrUserWithBuildings' does not exist on type 'Store<""brUser"", { currentUser: BrUser | undefined; brUsers: BrUser[]; isLoading: boolean; initialized: boolean; }, { getBrUser: (state: { currentUser: { [x: string]: unknown; userName?: string | undefined; ... 12 more ...; description?: string | undefined; } | undefined; brUsers: { ...; }[]; isLoading: boolean; init...'.
// FILE  C:/projects/beheerportaal/quasar/src/components/relations/UserBuildingManagement.vue:205:21

//    203 |   // ?? fix
//    204 |   //await organizationUserService.createOrganizationUser(user.value.id, organizationId); // todo organizationuser
//  > 205 |   await brUserStore.fetchBrUserWithBuildings(user.value.id);
//        |                     ^^^^^^^^^^^^^^^^^^^^^^^^
//    206 |
//    207 |   selectedBuilding.value = null;
//    208 | };

// ERROR(vue-tsc)  Property 'fetchBrUserWithBuildings' does not exist on type 'Store<""brUser"", { currentUser: BrUser | undefined; brUsers: BrUser[]; isLoading: boolean; initialized: boolean; }, { getBrUser: (state: { currentUser: { [x: string]: unknown; userName?: string | undefined; ... 12 more ...; description?: string | undefined; } | undefined; brUsers: { ...; }[]; isLoading: boolean; init...'.
// FILE  C:/projects/beheerportaal/quasar/src/components/relations/UserBuildingManagement.vue:214:21

//    212 |     await buildingUserService.delete(buildingId, userId, application.id ?? '');
//    213 |   }
//  > 214 |   await brUserStore.fetchBrUserWithBuildings(userId);
//        |                     ^^^^^^^^^^^^^^^^^^^^^^^^
//    215 |
//    216 |   // IPV HIERVAN HET OWNER OBJECT WIJZIGEN OF IIG DE LIJST
//    217 |   //await organizationBuildingService.deleteOrganizationBuilding(organizationId, buildingId, relation);

// ERROR(vue-tsc)  Property 'fetchBrUserWithOrganizations' does not exist on type 'Store<""brUser"", { currentUser: BrUser | undefined; brUsers: BrUser[]; isLoading: boolean; initialized: boolean; }, { getBrUser: (state: { currentUser: { [x: string]: unknown; userName?: string | undefined; ... 12 more ...; description?: string | undefined; } | undefined; brUsers: { ...; }[]; isLoading: boolean; init...'.
// FILE  C:/projects/beheerportaal/quasar/src/components/relations/UserOrganizationManagement.vue:123:21

//    121 | onMounted(async () => {
//    122 |   await brUserStore.fetchCurrentUser();
//  > 123 |   await brUserStore.fetchBrUserWithOrganizations(userId);
//        |                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    124 |   var u = brUserStore.brUsers.find((u) => u.id == userId);
//    125 |   if (!u || !u.organizations) return;
//    126 |

// ERROR(vue-tsc)  Property 'fetchBrUserWithOrganizations' does not exist on type 'Store<""brUser"", { currentUser: BrUser | undefined; brUsers: BrUser[]; isLoading: boolean; initialized: boolean; }, { getBrUser: (state: { currentUser: { [x: string]: unknown; userName?: string | undefined; ... 12 more ...; description?: string | undefined; } | undefined; brUsers: { ...; }[]; isLoading: boolean; init...'.
// FILE  C:/projects/beheerportaal/quasar/src/components/relations/UserOrganizationManagement.vue:173:21

//    171 |   // ?? fix
//    172 |   //await organizationUserService.createOrganizationUser(user.value.id, organizationId); // todo organizationuser
//  > 173 |   await brUserStore.fetchBrUserWithOrganizations(user.value.id);
//        |                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    174 |
//    175 |   selectedItem.value = null;
//    176 | };

// ERROR(vue-tsc)  Property 'fetchBrUserWithOrganizations' does not exist on type 'Store<""brUser"", { currentUser: BrUser | undefined; brUsers: BrUser[]; isLoading: boolean; initialized: boolean; }, { getBrUser: (state: { currentUser: { [x: string]: unknown; userName?: string | undefined; ... 12 more ...; description?: string | undefined; } | undefined; brUsers: { ...; }[]; isLoading: boolean; init...'.
// FILE  C:/projects/beheerportaal/quasar/src/components/relations/UserOrganizationManagement.vue:180:21

//    178 | const deleteItem = async (userId: string, organizationId: string) => {
//    179 |   await organizationUserService.deleteOrganizationUser(organizationId, userId); // TODO organizationuser
//  > 180 |   await brUserStore.fetchBrUserWithOrganizations(userId);
//        |                     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    181 |
//    182 |   // IPV HIERVAN HET OWNER OBJECT WIJZIGEN OF IIG DE LIJST
//    183 |   //await organizationBuildingService.deleteOrganizationBuilding(organizationId, buildingId, relation);

// ERROR(vue-tsc)  Property 'fetchSelectableInstallationCompanies' does not exist on type 'Store<""installationCompany"", { installationCompanies: InstallationCompany[]; isLoading: boolean; initialized: boolean; }, { getInstallationCompany: (state: { installationCompanies: { ...; }[]; isLoading: boolean; initialized: boolean; } & PiniaCustomStateProperties<...>) => (id: string) => InstallationCompany | unde...'. Did you mean 'fetchInstallationCompanies'?
// FILE  C:/projects/beheerportaal/quasar/src/pages/Manager/Contractor/ListPage.vue:18:26

//    16 |
//    17 | const installationCompanyStore = useInstallationCompanyStore();
//  > 18 | installationCompanyStore.fetchSelectableInstallationCompanies();
//       |                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    19 | const installationCompanies = computed(() => installationCompanyStore.installationCompanies);
//    20 |
//    21 |

// ERROR(vue-tsc)  Property 'fetchSelectableInstallationCompanies' does not exist on type 'Store<""installationCompany"", { installationCompanies: InstallationCompany[]; isLoading: boolean; initialized: boolean; }, { getInstallationCompany: (state: { installationCompanies: { ...; }[]; isLoading: boolean; initialized: boolean; } & PiniaCustomStateProperties<...>) => (id: string) => InstallationCompany | unde...'. Did you mean 'fetchInstallationCompanies'?
// FILE  C:/projects/beheerportaal/quasar/src/pages/Owner/Contractor/ListPage.vue:18:26

//    16 |
//    17 | const installationCompanyStore = useInstallationCompanyStore();
//  > 18 | installationCompanyStore.fetchSelectableInstallationCompanies();
//       |                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//    19 | const installationCompanies = computed(() => installationCompanyStore.installationCompanies);
//    20 |
//    21 |

//[vue-tsc] Found 14 errors. Watching for file changes.";


//static Tuple<string, string> GetQuasarBuildOutput()
//{
//    ProcessStartInfo psi = new ProcessStartInfo
//    {
//        WorkingDirectory = @"C:\projects\beheerportaal\quasar",
//        FileName = @"C:\projects\beheerportaal\quasar\runquasar.bat",
//        //Arguments = "build",
//        RedirectStandardOutput = true,
//        RedirectStandardError = true,
//        UseShellExecute = false
//    };

//    using (Process process = new Process { StartInfo = psi })
//    {
//        process.Start();

//        // Read output and errors
//        string output = process.StandardOutput.ReadToEnd();
//        string error = process.StandardError.ReadToEnd();
//        //var output = "";
//        //var error = "";

//        process.WaitForExit();

//        Console.WriteLine("Build Output:\n" + output);
//        Console.WriteLine("Build Errors:\n" + error);
//        var index = output.IndexOf("src/components/");
//        if (index == -1)
//            output = "";
//        else
//            output = output.Substring(index);

//        return new Tuple<string, string>(output, error);
//    }
//}

//for (var i = 0; i < 10; i++)
//{
//    var output = GetQuasarBuildOutput();
//    if (output.Item1 == "")
//        break;
//    var message = $@"Solve the first error in the build output. Make sure that the components keep looking and working the same as they did. The communication with the store needs to be properly configured.
//{output.Item1}
//{output.Item2}";
//    await generator.UpdateStore(message);
//    message = $@"Solve the second error in the build output:
//{output.Item1}
//{output.Item2}";
//    await generator.UpdateStore(message);
//    message = $@"Solve the third error in the build output:
//{output.Item1}
//{output.Item2}";
//    await generator.UpdateStore(message);
//}


//await generator.UpdateStore("Integrate the LocationHub into the LocationStore in the same way the BuildingHub works with the BuildingStore. Make sure that you refactor the way the class is setup into the useStore and defineStore pattern that the BuildingStore uses.");
//await generator.UpdateStore("Integrate the OwningCompanyHub into the OwningCompanyStore in the same way the BuildingHub works with the BuildingStore. Make sure that you refactor the way the class is setup into the useStore and defineStore pattern that the BuildingStore uses.");
//await generator.UpdateStore("Integrate the InstallationCompanyHub into the InstallationCompanyStore in the same way the BuildingHub works with the BuildingStore. Make sure that you refactor the way the class is setup into the useStore and defineStore pattern that the BuildingStore uses.");
//await generator.UpdateStore("Integrate the BuildingManagementCompanyHub into the BuildingManagementCompanyStore in the same way the BuildingHub works with the BuildingStore. Make sure that you refactor the way the class is setup into the useStore and defineStore pattern that the BuildingStore uses.");
//await generator.UpdateStore("Integrate the OrganizationHub into the OrganizationStore in the same way the BuildingHub works with the BuildingStore. Make sure that you refactor the way the class is setup into the useStore and defineStore pattern that the BuildingStore uses.");
//await generator.UpdateStore("Integrate the BrUserHub into the BrUserStore in the same way the BuildingHub works with the BuildingStore. Make sure that you refactor the way the class is setup into the useStore and defineStore pattern that the BuildingStore uses.");

////await generator.UpdateStore("Add a OwningCompanyHub that works like the BuildingHub but for OwningCompanies. The OwningCompanyHub should later be integrated in the OwningCompanyStore as a separate task");
////await generator.UpdateStore("Add a InstallationCompanyHub that works like the BuildingHub but for InstallationCompanies. The InstallationCompanyHub should later be integrated in the InstallationCompanyStore as a separate task");


////store.SaveEmbeddings();

////await generator.UpdateStore("create deposit.php");
////await generator.UpdateStore("create withdraw.php");
////await generator.UpdateStore("create investors.php");
////await generator.UpdateStore("create purchase.php");
////await generator.UpdateStore("create sell.php");
////await generator.UpdateStore("create expense.php");
////await generator.UpdateStore("create .htaccess");
////await generator.UpdateStore("Verify that the application is created correctly and update files where neccessary");
////await generator.UpdateStore("create caretakers.php");
////await generator.UpdateStore("create addcaretaker.php");
////await generator.UpdateStore("create company.php");

////await generator.UpdateStore("make sure deposit.php has the right menu and mobile layout");
////await generator.UpdateStore("make sure withdraw.php has the right menu and mobile layout");
////await generator.UpdateStore("make sure investors.php has the right menu and mobile layout");
////await generator.UpdateStore("make sure purchase.php has the right menu and mobile layout");
////await generator.UpdateStore("make sure sell.php has the right menu and mobile layout");
////await generator.UpdateStore("make sure expense.php has the right menu and mobile layout");
////await generator.UpdateStore("make sure caretakers.php has the right menu and mobile layout");
////await generator.UpdateStore("make sure addcaretaker.php has the right menu and mobile layout");
////await generator.UpdateStore("make sure company.php has the right menu and mobile layout");
////await generator.UpdateStore("create a tasks.txt. the first task: verify that all pages have a similar layout and for each page that doesnt have this layout create a task to update the layout for that page");
////await generator.UpdateStore("add a task: verify that all pages have a mobile friendly layout and for each page that doesnt have this layout create a task to update the layout for that page");
////await generator.UpdateStore("add a task: verify that all pages have the right menu items and for each page that doesnt have the right menu items create a task to update the menu for that page");
////await generator.UpdateStore("add a task: verify that all form input where an item is picked, like an animal by id, investor or caretaker or other items that are in a list that this input is a dropdown. for each page where this is not the case create a task to make it a dropdown");
////await generator.UpdateStore("add a task: verify that no pages have errors when running them and for each page that does have errors create a task to update the code for that page to make sure no errors exist");
////await generator.UpdateStore("verify if there is anything that still needs to be done and if so make tasks for that");

////for (var j = 0; j < 10; ++j)
////{
////    for (var i = 0; i < 3; ++i)
////    {
////        await generator.UpdateStore("pick the first task from tasks.txt that is not marked done, execute it and mark it as done");
////    }
////    await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");
////}

////await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");
////await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");

////await generator.UpdateStore("verify if there is anything that still needs to be done and if so make tasks for that");

////for (var i = 0; i < 3; ++i)
////{
////    await generator.UpdateStore("pick the first task from tasks.txt that is not marked done, execute it and mark it as done");
////}
////await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");
////await generator.UpdateStore("pick up to three tasks that are marked done, verify if they are really done and if there is anything still not good remove the done label and add a rework comment describing what still needs to be done. if the task is really done remove it completely from the list");
