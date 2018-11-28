# reficio

Reficio is a simple WPF program to convert plain text, rtf, and HTML files to PDF. It relys on the C# libraries [RtfPipe](https://github.com/erdomke/RtfPipe), and [Shark.PdfConvert](https://github.com/cp79shark/Shark.PdfConvert). We needed a better solution to convert large volumes of RTF files to PDF where I work. There are many expensive proprietary solutions that we have tried for this task.

This started largly as a project to learn C#. It was originally a console app but I was interested in learning a GUI framework. Since I was already learning C#, WPF made sense.

![Reficio](https://github.com/schemie/reficio/blob/master/reficio_01.png)

# Main Features

* Convert plain text, rtf, and HTML to PDF
* Configurable through the UI or through a config file
* Restartable (doesn't have to overwrite existing files)
* Multithread
  * Defaults to total cores minus two
  * If four cores or less, lets you select all four
* Real time statistics displayed in the GUI

# To-Do

* Update the main button click handling
   * 2nd click during processing stops the processing


