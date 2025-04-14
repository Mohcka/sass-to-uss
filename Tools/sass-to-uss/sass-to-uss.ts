// Use URLImport to reference dependencies without relying on local installations
import * as sass from "npm:sass@1.86.0"; // Lock to specific version
import { debounce } from "jsr:@std/async/debounce"; // Lock to specific version
import "npm:immutable@4.3.4"; // Add immutable dependency

const encoder = new TextEncoder();

// Function to convert SCSS/SASS to CSS
function convertSassToCss(sassFilePath: string): void {
  // Handle both .scss and .sass file extensions
  const cssFilePath = sassFilePath.replace(/\.(scss|sass)$/, '.uss');
  try {
    const result = sass.compile(sassFilePath);
    Deno.writeFileSync(cssFilePath, encoder.encode(result.css));
    console.log(`Compiled USS: ${cssFilePath}`);
  } catch (error) {
    console.error(`Error compiling ${sassFilePath}:`, error);
  }
}

// Function to check if file is a partial (starts with underscore)
function isPartial(filename: string): boolean {
  const basename = filename.split(/[\/\\]/).pop() || "";
  return basename.startsWith('_');
}

// Function to convert all SASS files in directory
async function convertAllFiles(directory: string): Promise<void> {
  console.log(`Converting all SASS files in: ${directory}`);
  
  try {
    for await (const entry of Deno.readDir(directory)) {
      if (entry.isDirectory) {
        // Recursively process subdirectories
        await convertAllFiles(`${directory}/${entry.name}`);
      } else if (entry.isFile) {
        // Process SASS files
        if (entry.name.endsWith('.scss') || entry.name.endsWith('.sass')) {
          const filePath = `${directory}/${entry.name}`;
          
          // Skip partials
          if (isPartial(entry.name)) {
            console.log(`Skipping partial file: ${entry.name}`);
            continue;
          }
          
          convertSassToCss(filePath);
        }
      }
    }
  } catch (error) {
    console.error(`Error processing directory ${directory}:`, error);
  }
}

const generateUss = debounce((event: Deno.FsEvent) => {
  // Check for both .scss and .sass files
  if (/\.(scss|sass)$/.test(event.paths[0]) && event.kind != 'remove') {
    // Extract filename from path
    const filename = event.paths[0].split(/[\/\\]/).pop() || "";
    
    // Skip files that start with underscore
    if (isPartial(filename)) {
      console.log(`Skipping partial file: ${filename}`);
      return;
    }
    
    console.log("[%s] %s", event.kind, event.paths[0]);
    convertSassToCss(event.paths[0]);
  }
}, 200);

// Get directory to watch from command line args or use current directory
const watchDir = Deno.args[0] || './';
const onceFlag = Deno.args.includes('--once');

// Check if we should do a one-time conversion or watch for changes
if (onceFlag) {
  // One-time conversion of all files
  convertAllFiles(watchDir.replace(/['"]/g, '')).then(() => {
    console.log("One-time conversion completed");
    Deno.exit(0);
  });
} else {
  // Watch for changes in SCSS/SASS files
  const watcher = Deno.watchFs(watchDir);
  (async () => {
    console.log(`Watching for SCSS/SASS file changes in: ${watchDir}`);
    for await (const event of watcher) {
      generateUss(event);
    }
  })();
}