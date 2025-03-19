// Use URLImport to reference dependencies without relying on local installations
import * as sass from "npm:sass@1.86.0"; // Lock to specific version
import { debounce } from "jsr:@std/async/debounce"; // Lock to specific version

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

const generateUss = debounce((event: Deno.FsEvent) => {
  // Check for both .scss and .sass files
  if (/\.(scss|sass)$/.test(event.paths[0]) && event.kind != 'remove') {
    console.log("[%s] %s", event.kind, event.paths[0]);
    convertSassToCss(event.paths[0]);
  }
}, 200);

// Get directory to watch from command line args or use current directory
const watchDir = Deno.args[0] || './';

// Watch for changes in SCSS/SASS files
const watcher = Deno.watchFs(watchDir);
(async () => {
  console.log(`Watching for SCSS/SASS file changes in: ${watchDir}`);
  for await (const event of watcher) {
    generateUss(event);
  }
})();