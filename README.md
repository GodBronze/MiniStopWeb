# MiniStop Web - E-commerce System

A fully functional e-commerce web application for a convenience store chain, built with ASP.NET Core MVC. This repository showcases both backend development and rigorous software testing methodologies.

## 🎯 Testing & QA Highlights

As a QA/QC Tester, my primary focus on this project was ensuring system stability, logical accuracy, and transaction integrity:

*   **Automated Testing (Selenium C#):** Developed automation scripts to verify the shopping cart logic. Specifically, validated that changing product quantity options dynamically updates the total price without direct element manipulation, accurately simulating real user behavior.
*   **Database Transaction Testing:** Verified ACID properties during the checkout flow. Ensured that when a payment is processed, the system correctly inserts records into the `HoaDon` and `ChiTietHoaDon` tables while precisely deducting inventory from the `SanPham` table.
*   **Data Mining Algorithm Verification:** Tested the backend recommendation engine powered by **Apriori** and **High-Utility Itemset Mining (HUIM)**. Validated that the system successfully recommends cross-selling products based on historical data and maximum profit margins, rather than just frequency.
*   **Manual Testing & Test Management:** Designed comprehensive test cases for order management and UI components using Microsoft Excel, tracking execution results to ensure full test coverage.

## 🛠️ Tech Stack
*   **Core:** C#, ASP.NET Core MVC, Entity Framework Core
*   **Testing:** Selenium WebDriver, Unit Testing concepts, Test Case Management
*   **Database:** Microsoft SQL Server (T-SQL, LINQ)
*   **Frontend:** HTML5, CSS3, Bootstrap 5, AJAX
