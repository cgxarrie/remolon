# ReMolon FrontEnd

# Users
Ability to login and register users, with different roles (Admin, Manager, StandardUser). Admins can manage retrospectives and users, Managers can manage retrospectives, and StandardUsers can participate in retrospectives.
Admins can change a user's role to any value.
Managers can change a user's role to Manager.

When a user role changes, the user should be logged out and required to log in again to refresh their permissions.


# Retrospectives
Display a list of available retrospectives by title.
Once a title is clicked, then list the retrospective's for that title with the start date, orderde by start date descending.
If no stat date, then use the proposed retrospective date.

Once a retrospective is selected, display the columns and items for that retrospective.

Actions on columns and items must be according to the user's role and the retrospective's status (open or closed).

Restrospectives can be created, edited, and deleted by Admins and Managers. When a retrospective is created, the user creating it is automatically assigned to it.

When a retrospective is closed, all items are locked and cannot be edited or deleted. Only Admins and Managers can close a retrospective.

When a retrospective is closed, the user who closed it is recorded in the database.


# Items
Items can be created, edited, and deleted by Admins and Managers. StandardUsers can only edit or delete items created by themselves. 
Items can be merged into other items by drag and drop onto the target item, and the user who performed the merge is recorded in the database.
Items can be reordered within a column by drag and drop, and the user who performed the reorder is recorded in the database.
Items can be moved between columns by drag and drop, and the user who performed the move is recorded in the database.

