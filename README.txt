POS MANAGEMENT
==============

Overview
--------
POS Management is a Windows desktop point-of-sale application built with WPF
and .NET 10. It supports product and category maintenance, checkout, stock
tracking, sales and receipt history, returns and refunds, and role-based user
administration. Transaction and inventory data are stored in Microsoft SQL
Server.

Main features
-------------
- Search products by name, SKU, or barcode and create a sales cart.
- Apply a sale discount and tax rate; accept cash, card, or mobile banking.
- Record sales, reduce stock, and keep stock movement history.
- Maintain product prices, stock, reorder levels, categories, and images.
- Find an invoice, process partial or full item returns, and optionally restock.
- Review sales and return summaries; print sales and return receipts.
- Manage Admin, Manager, and Cashier accounts with different access levels.

Requirements
------------
- Windows desktop (the project uses WPF and targets net10.0-windows).
- .NET 10 SDK to build from source.
- Microsoft SQL Server reachable from the computer running the application.
- The PosManagementDb database created and updated with the project's EF Core
  migrations before first launch.

Database configuration
----------------------
The SQL Server connection string is currently hard-coded in
PosManagement/App.xaml.cs and PosManagement/Data/ApplicationDbContextFactory.cs.
Both files currently name the SQL Server instance DESKTOP-94E2O8D and database
PosManagementDb, using Windows authentication. Change the server name and any
other connection options in both locations to match your environment. The
application does not automatically create the database or apply migrations.
Ensure the database is provisioned and migrations are applied by the person
responsible for the deployment before users launch the application.

Build and run
-------------
From the repository directory, with the .NET 10 SDK installed:

    dotnet restore PosManagement.slnx
    dotnet build PosManagement.slnx
    dotnet run --project PosManagement/PosManagement.csproj

Alternatively, open PosManagement.slnx in Visual Studio with the .NET 10
Windows desktop development components installed, set PosManagement as the
startup project, and run it.

First launch
------------
When the connected database has no users, the login window prompts you to
create the first administrator. Enter a full name, username, and password of
at least 8 characters, confirm the password, and choose Create Administrator.
Then log in with that account. The first account is assigned the Admin role.
The initial setup page appears only while the user table is empty.

Documentation
-------------
See USER_MANUAL.md for setup notes, role permissions, and step-by-step
instructions for checkout, inventory, returns, reports, and user management.

Support and operational notes
-----------------------------
- Keep the SQL Server database backed up according to your store's procedures.
- User passwords must be at least 8 characters. Five consecutive failed login
  attempts lock the account for five minutes.
- Product SKU and non-empty barcode values must be unique.
- Cash checkout requires the amount received to cover the total.
- This repository does not configure a payment processor, automatic database
  backup, or automatic schema migration.
