# NBrightMod Module

## Image Uploading Folder

The image upload folder is defined by the theme using the "settings.cshtml" template.  A textbox called "settinguploadfolder" is created and this is used for the image upload folder.

Example of code required to define the upload folder.

```
    <div class='dnnFormItem'>
        @DnnLabel("lblUploadFolder", "Settings.uploadfolder")
        @TextBox(info, "genxml/textbox/settinguploadfolder", "", "slider")
    </div>
```

The default for the textbox can be defined as the default of the textbox and will be applied to all new modules using this template.

If no "settinguploadfolder" textbox is found, then a default of "images" is used.

## List Header

An “App Theme” can have a list of records, each list may have a header record which can be used to inject data into the output html.  This header record is an independent record to the list data records.

To create a header record you need to add a "editlistheader.cshtml" template to the theme, under the Default folder.

This template is injected via the "editlist.cshtml" template.

Example of injecting the "editlistheader.cshtml" via the @RenderTemplate token.

```
@EditButtons("a,sl,ex")

@RenderTemplate(GetTemplateFilePath(Model.GetSetting("themefolder"), "editlistheader.cshtml", Model.GetSetting("modref"), "Default", true, true).ToString(), Model)

@RenderTemplate("/DesktopModules/NBright/NBrightMod/Themes/Shared/editlist-shared.cshtml", Model)
```

here is an example of a "editlistheader.cshtml" template:

```
@inherits NBrightMod.render.NBrightModRazorTokens<NBrightDNN.NBrightRazor>
@using System
@using System.Linq
@using NBrightDNN

@{
    var info = (NBrightInfo)Model.HeaderData;
    if (info == null)
    {
        info = new NBrightInfo();
    }
}

<div id="editdatalistheader">

    <!-- the "_h" is appended to the id, so we have unique ids for all hidden fields. -->
    <input id="lang_h" type="hidden" value="@info.Lang" />
    <input id="editlang_h" type="hidden" value="@info.Lang" />
    <input id="modref_h" type="hidden" value="@info.GUIDKey" />
    <input id="displayreturn_h" type="hidden" value="listheader" />
    <input id="moduleid_h" type="hidden" value="@info.ModuleId"/>

    <div>
        <div class='input-group'>
            @DnnLabel("lbltitle", "Edit.title")
            @TextBox(info, "genxml/textbox/title", "", "")
        </div>
        <div class='input-group'>
            @DnnLabel("lbltitle", "Edit.title")
            @TextBox(info, "genxml/lang/genxml/textbox/title", "class='form-control'")
            <span class="input-group-addon"><img src='/Images/Flags/@(info.Lang).gif' width='24px' /></span>
        </div>
        <div class='input-group'>
            @DnnLabel("lbltitle", "Edit.title")
            @NBrightTextBox(info, "genxml/lang/genxml/textbox/title2", "class='form-control'", "")
        </div>
    </div>

</div>

```



## Versioning

NBrightMod can allow a user to create a version of an update, without that version being visible to the public.  Once a Manager or user with validation rights has validated the change it is made visible to the public.
**Versioning is controlled by DNN roles permissions.**
Users with a role access that starts with "Version"  (e.g. "Version1") will not be able to update to the public.  When they update module content, the module will send an email to the "Manager" roles and all roles that have access to the modules that starts with "Validator" (e.g. Validator1)
Only when the users with "Manager" or "Validator" roles have accepted the change, will the change become visible to the public.


## Page Meta data and url page name

When a module displays a detail page, the page url link and page meta can be adapted.

For page URL Name:  Use input field  "genxml/lang/genxml/textbox/pagename"
For page Title:  Use input field  "genxml/lang/genxml/textbox/pagetitle"
For page Description:  Use input field  "genxml/lang/genxml/textbox/pagedescription"
For page keywords:  Use input field  "genxml/lang/genxml/textbox/pagekeywords"

"pagename" and "pagetitle" will fallback tp the non-langauge, if that is not there they will fallback to "genxml/lang/genxml/textbox/title"


