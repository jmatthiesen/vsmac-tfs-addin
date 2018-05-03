# Visual Studio for macOS TFS Add-in

Visual Studio for macOS add-in for interacting with Microsoft Team Foundation Server and Visual Studio Team Services.

## Usage

In Menu > Version Control section you will find the Team Foundation Server menu at the bottom. To start click on Connect to Server. 

<img src="images/add-remove-server.png" alt="Connect with a TFS Server" Width="600" />

 Choose between VSTS or TFVC server:

<img src="images/choose-server-type.png" alt="Connect with a TFS Server" Width="600" />

Enter you credentials: 

<img src="images/login.png" alt="Connect with a TFS Server" Width="600" />

The following authentication modes are supported:
- OAuth
- Basic
- Ntlm

Choose the projects thsat you want to be connected to: 

<img src="images/choose-projects.png" alt="Choose projects" Width="600" />

To continue, open "Team Explorer" from Menu > Version Control > Team Foundation Server > Team Explorer. 

<img src="images/teamexplorerpad.png" alt="Team Explorer" Width="300" />

Frome here, you can access to:
- SourceControlExplorer
- WorkItems

Double click on Source Control option to open SourceControlExplorerView. 

<img src="images/sourceexplorer.png" alt="Source Explorer" Width="800" />

Team Foundation Version Control (TFVC) is a centralized version control system. TFVC lets you do:
- Manage Workspaces (Create, edit or delete).
- Navigate between project structure.
- Map projects.
- Get projects.
- Lock & Unlock.
- Rename.
- Delete.
- Add new file.
- CheckOut.
- CheckIn.

From "Manage" button we can create a manage workspaces. 

<img src="images/manage-workspaces.png" alt="Manage Workspaces" Width="600" />

And create a new one:

<img src="images/create-workspace.png" alt="Create Workspace" Width="400" />

After creating a workspace, the actions are available in a contextual menu.

<img src="images/sourceexplorer-menu.png" alt="Create Workspace" Width="600" />

To work witk WorkItems, double click in WorkItems to open WorkItemsView.

<img src="images/workitems.png" alt="Create Workspace" Width="600" />

The WorkItems actions are available in a contextual menu.

<img src="images/workitems-menu.png" alt="Create Workspace" Width="600" />

**IMPORTANT:** The user interface design is temporary. Work in progress.

## Distribute

 To pack up the add-in and share with others, go to the assembly output folder to locate the add-in assembly, and call **vstool.exe** utility.

`$ mono /Applications/Visual\ Studio.app/Contents/Resources/lib/monodevelop/bin/vstool.exe setup pack MonoDevelop.VersionControl.TFS.dll`

<img src="images/vstool.png" alt="VSTool" Width="500" />

## Thanks

- [https://github.com/Indomitable/monodevelop-tfs-addin](https://github.com/Indomitable/monodevelop-tfs-addin)
- [https://github.com/Microsoft/vsts-auth-samples](https://github.com/Microsoft/vsts-auth-samples)
- [https://github.com/AzureAD/azure-activedirectory-library-for-dotnet](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet)

## Contributing

We take contributions and fixes via Pull Request. Are you interesting?. You can **[contribute](docs/How-Contribute.md)**!.

## Copyright and license

Code and documentation copyright 2018 Microsoft Corp. Code released under the [MIT license](https://opensource.org/licenses/MIT).

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
