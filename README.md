# ShopRAR – Retail Management System

ShopRAR is a multi-layered .NET application designed to demonstrate clean architecture and database interaction using both **Entity Framework** and **Stored Procedures**. The project provides a structured approach to building scalable and maintainable applications.

## Project Architecture

The solution follows a layered architecture:

### Layer Responsibilities

**1. UI Layer (ShopRAR.UI)**
- Handles user interaction
- Displays data to the user
- Sends requests to the business logic layer

**2. Business Logic Layer (BLL)**
Contains application logic and data processing.

- **ShopRAR.BLL.EF**
  - Uses Entity Framework
  - Handles database operations through ORM

- **ShopRAR.BLL.SP**
  - Uses SQL Stored Procedures
  - Directly interacts with the database

**3. Domain Layer**
- Contains entity models
- Represents database tables and business objects

**4. SQL Scripts**
- Database schema
- Stored procedures used in the project

---

## Technologies Used

- C#
- .NET Framework / .NET
- Entity Framework
- SQL Server
- Stored Procedures
- Visual Studio

---

## Database Setup

1. Open **SQL Server Management Studio (SSMS)**.
2. Create a new database.
3. Run the following scripts from the `sql` folder:


These scripts will create the database schema and required stored procedures.

---

## Running the Project

1. Open **ShopRAR.sln** in **Visual Studio**.
2. Restore NuGet packages if prompted.
3. Update the **database connection string** in the configuration file.
4. Build the solution.
5. Run the project.

---

## Features

- Layered architecture
- Entity Framework database operations
- Stored procedure-based database access
- Modular and maintainable code structure
- SQL scripts for database setup

---

## Learning Objectives

This project demonstrates:

- Multi-layered software architecture
- Separation of concerns
- Database interaction using different techniques
- Integration of SQL Server with .NET applications

---

## Future Improvements

- Add authentication and authorization
- Improve UI design
- Implement API layer
- Add logging and validation
- Deploy to cloud environment

---

## Authors

Group 20

---

## License

This project is for educational purposes.
