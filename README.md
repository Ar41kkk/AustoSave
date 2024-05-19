# Backup Manager Application

Backup Manager Application is a comprehensive solution designed to automate the process of backing up files from a specified source directory to a designated destination directory at regular intervals. This Windows Forms application leverages asynchronous programming to ensure efficient file copying operations while maintaining a responsive user interface.

## Features

1. **Automated Backups**: Schedule and run automatic backups at user-defined intervals.
2. **Customizable Settings**: Load and save backup settings, including source path, destination path, and backup interval, from/to a JSON file.
3. **Concurrency Control**: Utilize semaphore to limit the number of concurrent backup operations, ensuring efficient resource usage.
4. **Pause and Resume**: Start and stop the backup process with ease.
5. **Tray Icon Integration**: Minimize the application to the system tray, with the ability to restore or exit from the tray icon context menu.
6. **File Selection**: Select and manage a list of specific files to be backed up.
7. **Visual Indicators**: Display the current status of the backup process using colored indicators (green for running, red for stopped).
8. **Logging**: Maintain a log of backup activities, including successes and errors, to a log file.
9. **User-Friendly Interface**: Simple and intuitive interface for configuring backup settings and managing files.
10. **Error Handling**: Comprehensive error handling to manage exceptions and provide feedback to the user.

## Getting Started

1. **Clone the Repository**: Clone this repository to your local machine using `git clone <repository-url>`.
2. **Open the Solution**: Open the solution file in Visual Studio.
3. **Build the Solution**: Build the solution to restore all necessary packages and dependencies.
4. **Run the Application**: Start the application by running the `Form1` project.

## Usage

- **Configure Backup Settings**: Use the interface to select the source and destination directories and set the backup interval.
- **Manage Files**: Add or remove files from the backup list using the `AustoSave` form.
- **Start Backup**: Click the start button to initiate the backup process. The application will run in the background, performing backups at the specified interval.
- **Monitor Status**: Check the backup status via the status label and color indicator on the main form.
- **System Tray**: Minimize the application to the system tray for unobtrusive operation. Double-click the tray icon to restore the application.

## Contributions

To contribute to this project, please create a pull request with a detailed description of your changes. Ensure your code adheres to the existing coding standards and is well-documented.