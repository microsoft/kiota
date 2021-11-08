import { assert } from "chai";

import { JsonParseNodeFactory } from "../../src/jsonParseNodeFactory"

import { Response } from "node-fetch";

describe("jsonParseNodeFactory", () => {
    it("jsonParseNodeFactory", async () => {
        const jsonBody = {
            x: "TEST",
            y: 111
        };
        const response = new (Response as any)(JSON.stringify(jsonBody));

        const arrayBuffer = await response.arrayBuffer();
        const s = new JsonParseNodeFactory()
        // const node = await (new JsonParseNodeFactory())["getRootParseNode"]("application/json",arrayBuffer);

        assert.deepEqual(s["convertToJson"](arrayBuffer), jsonBody);
    });
});