# NBrightMod Module

## List Header

An “App Theme” can have a list of records, each list may have a header record which can be used to inject data into the output html.  This header record is an independent record to the list data records.

To create a header record you need to add a "editlistheader.cshtml" template to the theme, under the Default folder.

here is an example of a template:



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


