# BugCacheConnection
Exception thrown when querying many byte array table rows

# Reproduced issue specs
- Target Framework: net6.0
- InterSystemsCache Package: 6.0.0
- InterSystemsCache: 2018.1.3.414.0

#How to reproduce the bug

1. Import the global MCI.Student that can be found within the project as 'export.gof';
2. Set the --connectionString and --id arguments into launchSettings.json file;
	1. --connectionString: Required argument to connect to the database;
	2. --id: Required argument to run a query with a "Where ID = ?" condition;
	3. --imagePath: Optional argument. It can be used to insert new rows in table with any image;
3. Execute the program;
4. The exception shall be thrown. Otherwise, check the path argument, database rows and the console application; 

# About the bug: 
As we noticed, the bug occurs when the program makes a query without WHERE condition in a table that:
1. Has a GlobalStream column;
2. Has at least (about) 20 rows that contains value in GlobalStream column (in our tests this number was enough to reproduce. It does not reproduce with less than that);
3. The query has not a WHERE condition in the query (for some reason, when we use a WHERE condition to return one row only, it works correctly);
4. The query returns at least 6 columns (for some reason, when we select few columns, it works correctly);
