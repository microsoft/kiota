const fs = require("fs");
const path = require("path");
const ts = require("typescript");

const libDir = path.resolve(__dirname, "../lib");
const readmePath = path.resolve(__dirname, "../readme.md");

let markdown = `# @microsoft/kiota

This library provides various functions to interact with Kiota, a client generator for HTTP REST APIs described by OpenAPI.

## Installation

To install the package, use the following command:

\`\`\`bash
npm install @microsoft/kiota
\`\`\`

## Usage

Once installed, you can use the available functions to generate and interact with Kiota clients.
Below is a reference for each function, including parameters and return values.

`;

let tsFilePaths;
try {
  tsFilePaths = fs
    .readdirSync(libDir)
    .filter((file) => file.endsWith(".ts"))
    .map((file) => path.join(libDir, file));
} catch (err) {
  console.error(`Error reading directory: ${err.message}`);
  process.exit(1);
}

for (const filePath of tsFilePaths) {

  let fileContent;
  try {
    fileContent = fs.readFileSync(filePath, "utf-8");
  } catch (err) {
    console.error(`Error reading file ${filePath}: ${err.message}`);
    continue;
  }

  const sourceFile = ts.createSourceFile(filePath, fileContent, ts.ScriptTarget.Latest, true);

  ts.forEachChild(sourceFile, (node) => {
    if (ts.isFunctionDeclaration(node) && node.name) {
      const functionName = node.name.text;
      markdown += `### \`${functionName}\`\n\n`;

      const jsDocs = node.jsDoc || [];
      const jsDocText = jsDocs.map(doc => doc.comment).filter(Boolean).join("\n\n");
      if (jsDocText) {
        markdown += `${jsDocText}\n\n`;
      }

      const { paramLines, returnLine, throwsLines } = extractJsDocInfo(jsDocs);
      if (paramLines.length) markdown += `**Parameters:**\n\n${paramLines.join("\n")}\n\n`;
      if (returnLine) markdown += `${returnLine}\n`;
      if (throwsLines.length) markdown += `**Throws:**\n${throwsLines.join("\n")}\n\n`;
    }
  });
}

function extractParamName (nameNode) {
  if (!nameNode) return "";

  const rawParamText = nameNode.getText();
  const paramNameMatch = rawParamText.match(/@param\s+{.*?}\s+(\[.*?\])/);
  const fullParamName = paramNameMatch && paramNameMatch[1] ? paramNameMatch[1] : nameNode.name.getText() || "";
  return fullParamName;
}

function parseOptionalParam (rawName) {
  const match = rawName.match(/^\[([^\]]+)]$/);
  return match ? {
    name: match[1], optional: true
  } : {
    name: rawName, optional: false
  };
}

function extractJsDocInfo (jsDocs) {
  let throwsLines = [];
  let returnLine = null;
  let paramLines = [];
  let requiredParams = [];
  let optionalParams = [];

  jsDocs.forEach(doc => {
    (doc.tags || []).forEach(tag => {
      const tagName = tag.tagName.escapedText;

      if (tagName === "param") {
        const paramType = tag.typeExpression?.type?.getFullText().trim() || "";
        const fullParamName = extractParamName(tag);
        const { name, optional } = parseOptionalParam(fullParamName);

        const formattedParam = `- \`${name}\`${optional ? " (optional)" : ""}: ${paramType ? `*${paramType}*` : ""} ${tag.comment || ""}`;

        optional ? optionalParams.push(formattedParam) : requiredParams.push(formattedParam);
      }

      if (tagName === "returns") returnLine = `**Returns:** ${tag.comment}\n`;
      if (tagName === "throws") throwsLines.push(`- ${tag.comment}`);
    });
  });
  paramLines = requiredParams.concat(optionalParams.length ? [""] : []).concat(optionalParams);
  return { paramLines, returnLine, throwsLines };
}

try {
  fs.writeFileSync(readmePath, markdown);
  console.log("readme.md generated!");
} catch (err) {
  console.error(`Error writing file ${readmePath}: ${err.message}`);
}
