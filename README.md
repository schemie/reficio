# reficio

Reficio is a simple WPF program to convert plain text, rtf, and HTML files to PDF. It relys on the C# libraries [RtfPipe](https://github.com/erdomke/RtfPipe), and [Shark.PdfConvert](https://github.com/cp79shark/Shark.PdfConvert). We needed a better solution to convert large volumes of RTF files to PDF where I work. There are many expensive proprietary solutions that we have tried for this task.

This started largly as a project to continue learning C#. It was originally a console app but I was interested in learning a GUI framework. Since I was already learning C#, WPF made sense. I am also using [Material Design in XAML](http://materialdesigninxaml.net/) to make the UI nicer to look at.

![Reficio](https://github.com/schemie/reficio/blob/master/reficio_01.png)

# Main Features

* Convert plain text, rtf, and HTML to PDF
* Configurable through the UI or through a config file
* Restartable if it crashes/the computer restarts (doesn't have to overwrite existing files)
* Copy and convert the selected folder along with all sub-folders and files within
* Multithread
  * Defaults to total cores minus two
  * If four cores or less, lets you select all four
* Real time statistics displayed in the GUI

# How To Use

* Download and install [wkhtmnltopdf](https://wkhtmltopdf.org/downloads.html)
* Download and unzip [reficio](https://github.com/schemie/reficio/releases)
* To set paths and options through the config file open and edit "reficio.exe.config" in the unzipped folder
* To set paths and options through the UI launch "reficio.exe"
* Click the blue button and wait for the processing to finish

# To-Do

* Update the main button click handling
   * 2nd click during processing stops the processing
* Make launchable as a console app or GUI app
* Have run into a small amount of RTF files the RtFPipe library can't handle
  * If I can figure out the issues, create a pull request to fix it


