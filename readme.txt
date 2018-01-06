NBrightMod Module
-----------------

Versioning
----------

NBrightMod can allow a user to craete a version of an update, without that version being visible to the public.  Once a Manager or user with validation rights has validated the change it is made visible to the public.

Versioning is controlled by DNN roles permissions.

Users with a role access that starts with "Version"  (e.g. "Version1") will not be able to update to the public.  When they update module content, the module will send an email to the "Manager" roles and all roles that have access to the modules that starts with "Validator" (e.g. Validator1)

Only when the users with "Manager" or "Validator" roles have accepted the change, will the change become visible to the public.

