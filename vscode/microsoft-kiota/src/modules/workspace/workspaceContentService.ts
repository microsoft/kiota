import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

import { WorkspaceContent } from ".";
import { getWorkspaceJsonPath } from '../../util';

class WorkspaceContentService {
  constructor() { }

  public async load(): Promise<WorkspaceContent | undefined> {
    const isWorkspacePresent = await this.isKiotaWorkspaceFilePresent();
    if (!isWorkspacePresent) {
      return;
    }
    try {
      const content = await fs.promises.readFile(getWorkspaceJsonPath(), 'utf-8');
      return JSON.parse(content);
    } catch (error) {
      console.error('Error loading workspace.json:', error);
    }
  }

  async isKiotaWorkspaceFilePresent(): Promise<boolean> {
    if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
      return false;
    }
    const workspaceFileDir = path.resolve(getWorkspaceJsonPath());
    try {
      await fs.promises.access(workspaceFileDir);
    } catch (error) {
      return false;
    }
    return true;
  }
}

export default WorkspaceContentService;