# POS Management User Manual

## 1. About this manual

POS Management is a Windows desktop application for retail checkout and basic
store operations. It manages a product catalog and stock, records sales and
returns, and provides role-based access for staff. The screens and steps below
describe the current application.

## 2. Before you start

The application requires:

- A Windows computer that can run a WPF desktop application targeting
  `net10.0-windows`.
- A reachable Microsoft SQL Server instance.
- A `PosManagementDb` database with the application's Entity Framework Core
  migrations already applied.
- A Windows account with the required database connection permissions.

The connection string is currently defined in
`PosManagement/App.xaml.cs` and `PosManagement/Data/ApplicationDbContextFactory.cs`.
It currently points to `DESKTOP-94E2O8D` with Windows authentication. The
deployment administrator must update both definitions if the SQL Server
instance differs, provision the database, and apply migrations before opening
the application. The application does not perform those setup tasks
automatically. If you build from source, install the .NET 10 SDK, then run:

```powershell
dotnet restore PosManagement.slnx
dotnet build PosManagement.slnx
dotnet run --project PosManagement/PosManagement.csproj
```

## 3. First administrator setup and sign-in

1. Start POS Management and wait for the login window to connect to the
   database.
2. If the database has no user accounts, the **Initial Administrator Setup**
   form appears. Enter your full name and a username.
3. Enter and confirm a password of at least 8 characters, then select **Create
   Administrator**.
4. After the success message, enter the new username and password in **User
   Login**, then select **Login**.
5. The POS Sales page opens after successful sign-in.

The first account is assigned the Admin role. Once a user exists, first-time
setup is no longer shown. After five consecutive incorrect password attempts,
the account is locked for five minutes. An inactive account cannot sign in; an
administrator must reactivate it.

## 4. Roles and access

| Role | Available areas |
| --- | --- |
| Admin | POS / Sales, Products, Sales History, Return / Refund, Return History, User Management |
| Manager | POS / Sales, Products, Sales History, Return / Refund, Return History |
| Cashier | POS / Sales, Sales History, Return / Refund |

Cashiers can see their own sales in Sales History. Admins and Managers can see
all sales. Return history and product maintenance are available to Admins and
Managers. Only Admins can create, edit, deactivate, and reset staff accounts.

Use the navigation on the left to open a page. The signed-in name and role
appear at the bottom of the navigation area. Use the power icon there to log
out; confirm the prompt. Logging out returns to the login window.

## 5. Make a sale

1. Open **POS / Sales**. The product list shows active items that can be sold.
2. Search by product name or SKU, or enter/scan a barcode into the search box.
   Select the matching product. Select **Add Selected Product** to add one
   unit. Select it again to increase the quantity by one, up to available
   stock.
3. In **Shopping Cart**, select a line and choose **Remove Item** to remove it.
   Edit a line's **Qty** cell to change its quantity. **Clear Cart** empties the
   cart and resets the discount and amount received.
4. Review the subtotal. Enter a discount amount and a tax rate percentage if
   applicable. The screen calculates tax and total. The tax rate starts at
   zero for a new cart.
5. Choose **Cash**, **Card**, or **Mobile Banking** as the payment method. For
   cash, enter the amount received; the change due is calculated for you. Cash
   checkout is blocked when the amount received is below the total. Card and
   mobile banking checkout record the total as received.
6. Review the items, quantities, discount, tax, payment method, and total.
   Select **Complete Sale** and wait for the completion message. The sale is
   recorded, stock is reduced, and the cart is cleared. Note the invoice number
   for receipt lookup or returns.

The application checks stock again when the sale is saved. If another
transaction changes stock first, checkout reports the conflict; refresh the
product list and retry with the available quantity. A completed sale is
recorded in Sales History.

## 6. Maintain products and categories

Open **Products** (Admin or Manager).

### Add a product

1. Select **New** to clear the form.
2. Enter product name, SKU, optional barcode, cost price, selling price,
   current stock, and reorder level. Choose a category if available.
3. To add a category, enter its name in **New Category** and select **Add**.
   The new category is selected for the product.
4. To attach an image, select **Browse** and choose a JPG, JPEG, PNG, BMP, or
   GIF file. The image change takes effect when the product is saved.
5. Select **Save Product**. Review the status message for validation errors
   or confirmation.

### Edit, search, and deactivate

- Select a row in the product list to load it into the form. Change its fields
  and select **Save Product**.
- Search the product list by name, SKU, or barcode.
- **Deactivate** marks a product inactive so it is excluded from the POS
  product list. It preserves the product record and its past sale history.
- Use **Remove** in the image section to clear the product image, then save.

SKUs must be unique. A non-empty barcode must also be unique. Enter valid
numeric values for prices, stock, and reorder level. Stock cannot be negative.
The list reports stock as **In Stock**, **Low Stock**, or **Out of Stock** using
the configured reorder level.

## 7. Review sales and print receipts

Open **Sales History**. The list is sorted newest first and displays up to the
500 most recent sales available to your role. Cashiers see only their own
sales; Admins and Managers see all sales. Select **Refresh** to reload the
list.

To print a receipt, select the sale row and choose **Print Receipt**. In the
Windows print dialog, choose a printer and print settings. The receipt includes
the invoice, date, payment, line items, discount, tax, total, received amount,
and change. A printer must be configured in Windows to print a physical copy.

## 8. Process a return or refund

Open **Return / Refund** (Admin, Manager, or Cashier).

1. Enter the invoice number from the original sale and select **Search**.
2. Review the sold quantity, quantities already returned, and remaining
   available quantity for each item.
3. Select the **Return** checkbox for each item to return. Enter a return
   quantity no greater than the available quantity. Items not selected are not
   included.
4. For each selected item, choose whether to **Restock** it. Restocked items
   are added back to inventory; clear the option when the returned item should
   not be returned to sellable stock.
5. Enter a reason if useful, choose a refund method (Cash, Card, or Mobile
   Banking), and review the refund total.
6. Select **Process Return** and confirm the details in the confirmation
   prompt. Wait for the success message.

The refund is calculated from the original sale item's unit price multiplied
by the returned quantity. The application prevents returning more units than
were sold minus previous returns. A return may include selected lines from an
invoice, and the remaining eligible quantities can be returned later.

## 9. Review return history

Open **Return History** (Admin or Manager). Enter an invoice number and/or set
a start date and end date, then select **Apply**. The date range includes both
selected dates. Select **Clear** to remove all filters. The page shows summary
totals, matching return records, details for the selected record, daily
refund totals, and totals grouped by refund method. The detail view indicates
whether each returned item was restocked.

Select a return record and choose **Print Return Receipt** to print its return
receipt using the Windows printer dialog.

## 10. Manage staff accounts (Admin only)

Open **User Management**.

### Create an account

1. Select **New User**.
2. Enter a unique username, full name, and initial password of at least 8
   characters.
3. Choose Admin, Manager, or Cashier and leave **Active account** checked if
   the user should be able to sign in.
4. Select **Save User**.

### Edit, deactivate, or reset a password

- Select a user in the list to load their details. Update the username, full
  name, role, or active status, then select **Save User**.
- To change an existing user's password, select that user, enter a new password
  (at least 8 characters), and choose **Reset Password**.
- An inactive user cannot sign in. Reactivate the account by checking **Active
  account** and saving.
- You cannot deactivate your own account or remove the Admin role from your
  own account. Use care when changing the role of other administrators.

## 11. Troubleshooting

| Message or symptom | What to check |
| --- | --- |
| Startup or login cannot connect to the database | Confirm SQL Server is running and reachable, the database exists and migrations have been applied, and the connection string in both configuration files matches the server. Check Windows authentication permissions. |
| Initial administrator setup fails | Confirm the user table is empty and the database connection is working. Setup is permitted only once per empty user table. |
| Invalid username or password | Check spelling and keyboard layout. After five failed attempts, wait five minutes or ask an administrator for help. |
| Product is missing from POS | Search again, verify that it is active, and check stock. Inactive products are excluded; out-of-stock products cannot be added. |
| Checkout says stock changed or insufficient | Refresh the POS product list, adjust cart quantity to available stock, then retry. |
| Invoice cannot be found for a return | Verify the full invoice number from Sales History or the printed receipt. |
| No receipt prints | Confirm a Windows printer is installed, online, and selected in the print dialog. |

## 12. Data and operational care

Sales, returns, product records, staff accounts, and stock are stored in SQL
Server. Follow your organization's database backup and retention procedures.
The software does not configure automatic backups, payment processing, or
automatic schema migration. Coordinate database changes and connection-string
updates with the person responsible for the deployment.
