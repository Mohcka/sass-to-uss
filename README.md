# SASS to USS for Unity

A Unity tool that watches SCSS files and compiles them to USS files for UI Toolkit.

##### Currently only works on Windows

## Installation

1. Open Unity Package Manager
2. Click the "+" button in the top-left corner
3. Select "Add package from git URL"
4. Enter the git URL: `https://github.com/Mohcka/sass-to-uss.git`
5. Click "Add"
6. Access the tool via Tools > SASS to USS

## Building the SASS to USS Executable (Optional)

1. Install Deno: https://deno.land/
2. Navigate to the Tools/sass-to-uss folder
3. Run: `deno compile --allow-read --allow-write --allow-net sass-to-uss.ts`
4. Place the compiled executable in the same folder

## Usage

1. Open the SASS to USS window
2. Enter the directory containing your SCSS files
3. Click "Start Converting"
4. The tool will automatically compile SCSS to USS when files change
