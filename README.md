# SASS to USS for Unity

A Unity tool that watches SCSS files and compiles them to USS files for UI Toolkit.

##### Currently only works on Windows

## Installation

1. Import this package into your Unity project
2. Access the tool via Tools > SASS to USS

## Building the SASS to USS Executable

If you need to rebuild the executable:

1. Install Deno: https://deno.land/
2. Navigate to the Tools/sass-to-uss folder
3. Run: `deno compile --allow-read --allow-write --allow-net sass-to-uss.ts`
4. Place the compiled executable in the same folder

## Usage

1. Open the SASS to USS window
2. Enter the directory containing your SCSS files
3. Click "Start Converting"
4. The tool will automatically compile SCSS to USS when files change
