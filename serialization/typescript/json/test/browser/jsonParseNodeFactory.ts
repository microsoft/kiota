import { assert } from "chai";

import { JsonParseNodeFactory } from "../../src/browser/jsonParseNodeFactory"

describe("jsonParseNodeFactory", () => {
    it("jsonParseNodeFactory", async () => {
        const jsonBody = {
            x: "TEST",
            y: 111
        };
        const response = new Response(JSON.stringify(jsonBody));

        const arrayBuffer = await response.arrayBuffer();
        const s = new JsonParseNodeFactory();
        assert.deepEqual(s["convertToJson"](arrayBuffer) as unknown, jsonBody);
    });
});