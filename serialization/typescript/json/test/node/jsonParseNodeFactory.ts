import { describe } from "mocha";
import { assert } from "chai";

import {JsonParseNodeFactory} from "../../src/jsonParseNodeFactory"

import {Response} from "node-fetch";


// import { json } from "stream/consumers";
// import Sinon from "sinon";

describe("jsonParseNodeFactory", () => {
    it("jsonParseNodeFactory", async() => {
        const jsonBody = {
            x: "TEST",
            y: 111
        };
        const response = new Response(JSON.stringify(jsonBody));

        const arrayBuffer = await response.arrayBuffer();
        const s= new JsonParseNodeFactory()
       // const node = await (new JsonParseNodeFactory())["getRootParseNode"]("application/json",arrayBuffer);

         assert.equal(s["convertToJson"](arrayBuffer), jsonBody);
        // const spy = Sinon.spy(s, "getRootParseNode");
        // assert.isDefined(spy.arguments);


    });
});