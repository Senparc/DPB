# DPB: Dynamic Project Builder

DPB is a tool that allows developers to automatically generate project code. 

You can add annotations to the code templates, and use DPB to automatically filter or generate code to build a complete new project.

## Why I need DPB

We always build customized projects and template projects, while new project requirements comming, we have to copy the whole project files and do a lot of <i>washing</i> jobs: remove the files, change the key contents, modify configuration values and so on.

Also when we published an open source project, we always support Demo / Sample and Documents. But really not every developer wants to read all of the code and sample project and documents.

With DPB, you just need to put same marks into your code, such as `PDBMARK Keep` and 'PDBMARK_END', then run DPB, it will build a new clearn-project into the Output Directory, just keep the code and files you want.
