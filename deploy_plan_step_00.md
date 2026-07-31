# Step 00 - Cost Safety Nets (Beginner Guide)

Goal: protect yourself from accidental Azure charges before creating resources.

## What you will do

1. Open the Azure Portal.
2. Create a budget for your subscription.
3. Create spending alerts.
4. Enable cost anomaly alerts.

## Prerequisites

1. Azure account and active subscription.
2. Permission to manage costs on your subscription.

## Exact steps

1. Sign in to Azure:
   - Go to https://portal.azure.com
   - Log in with your Azure account.
2. Open Cost Management:
   - In the top search bar, type "Cost Management + Billing".
   - Open the service.
3. Select your subscription scope:
   - In Cost Management, confirm you are viewing the correct subscription.
4. Create a budget:
   - Go to Cost Management -> Budgets.
   - Click Add.
   - Name: `retromolon-budget`.
   - Reset period: Monthly.
   - Budget amount: `1` USD.
   - Click Next.
5. Add alert thresholds:
   - Add alert at 50%.
   - Add alert at 80%.
   - Add alert at 100%.
   - Set your email as recipient.
6. Save the budget:
   - Review settings.
   - Click Create.
7. Enable anomaly alerts:
   - In Cost Management, open Cost alerts.
   - Enable anomaly detection notifications if available.

## Verification checklist

1. You can see `retromolon-budget` in Budgets.
2. You can see 3 thresholds (50, 80, 100).
3. Notification email is correctly configured.

## Common mistakes

1. Creating budget at the wrong scope.
2. Forgetting to add email recipients.
3. Using a high budget that defeats early warning.

## Done when

- Budget and alerts are active before any deployment work starts.
