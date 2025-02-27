import * as vscode from 'vscode';
import * as filterModule from "../../../commands/openApiTreeView/filterDescriptionCommand";
import * as filterStepsModule from "../../../modules/steps/filterSteps";
import * as treeModule from "../../../providers/openApiTreeProvider";

describe('FilterDescriptionCommand Test Suite', () => {
    void vscode.window.showInformationMessage('Start FilterDescriptionCommand tests.');

    afterEach(async () => {
        jest.clearAllMocks();
    });

    test('test function getName of filterDescriptionCommand', () => {
        const treeProvider = jest.createMockFromModule<treeModule.OpenApiTreeProvider>("../../../providers/openApiTreeProvider");
        const filterDescriptionCommand = new filterModule.FilterDescriptionCommand(treeProvider);
        expect("kiota.openApiExplorer.filterDescription").toEqual(filterDescriptionCommand.getName());
    });

    test('test function execute of filterDescriptionCommand', async () => {
        const filterStepsStub = jest.spyOn(filterStepsModule, 'filterSteps').mockResolvedValue({});
        const treeProvider = jest.createMockFromModule<treeModule.OpenApiTreeProvider>("../../../providers/openApiTreeProvider");
        const filterDescriptionCommand = new filterModule.FilterDescriptionCommand(treeProvider);
        await filterDescriptionCommand.execute();
        expect(filterStepsStub).toHaveBeenCalledTimes(1);
        expect(filterStepsStub).toHaveBeenCalledWith(treeProvider.filter, expect.any(Function));
    });
});