import assert from "node:assert/strict";
import { JsonParseNode, JsonSerializationWriter } from "@microsoft/kiota-serialization-json";
import {
    createBaseFromDiscriminatorValue,
    createReferenceModelFromDiscriminatorValue,
    deserializeIntoBase,
    deserializeIntoOptionalChoice,
    deserializeIntoRequiredChoice,
    deserializeIntoReferenceModel,
    deserializeIntoWidget,
    serializeOptionalChoice,
    serializeRequiredChoice,
    serializeReferenceModel,
    serializeWidget,
    type Widget,
    type ReferenceModel,
} from "./client/models/index.js";
import * as models from "./client/models/index.js";

const reference = new JsonParseNode({ localName: "reference", inlineName: "inline", secondName: "second", inheritedName: "base" })
    .getObjectValue<ReferenceModel>(createReferenceModelFromDiscriminatorValue);
assert.ok(reference);
assert.equal(reference.localName, "reference");
assert.equal(reference.inlineName, "inline");
assert.equal(reference.inheritedName, "base");
assert.equal(reference.secondName, "second");
assert.equal("localDrop" in deserializeIntoReferenceModel({}), false);
assert.equal("inlineDrop" in deserializeIntoReferenceModel({}), false);
assert.equal("secondDrop" in deserializeIntoReferenceModel({}), false);
const referenceWriter = new JsonSerializationWriter();
referenceWriter.writeObjectValue(undefined, reference, serializeReferenceModel);
const referenceJson = JSON.parse(new TextDecoder().decode(referenceWriter.getSerializedContent()));
assert.equal(referenceJson.localName, "reference");
assert.equal(referenceJson.inlineName, "inline");
assert.equal(referenceJson.inheritedName, "base");
assert.equal(referenceJson.secondName, "second");
assert.equal("localDrop" in referenceJson, false);
assert.equal("inlineDrop" in referenceJson, false);
assert.equal("secondDrop" in referenceJson, false);

const payload = {
    id: "base-id",
    kind: "widget",
    marker: "base-kept",
    name: "kept",
    extra: "preserved",
    choice: { name: "optional" },
    friends: [{ id: "friend-id", kind: "widget", name: "friend" }],
};
const widget = new JsonParseNode(payload).getObjectValue<Widget>(createBaseFromDiscriminatorValue);
assert.ok(widget);
assert.equal(widget.id, "base-id");
assert.equal(widget.name, "kept");
assert.equal(widget.marker, payload.marker);
assert.equal(widget.friends?.[0]?.id, "friend-id");
assert.ok(widget.choice && "name" in widget.choice);
assert.equal(widget.choice.name, "optional");
assert.equal("baseOptional" in deserializeIntoBase({}), false);
assert.equal("drop" in deserializeIntoWidget({}), false);
assert.equal("profile" in deserializeIntoWidget({}), false);
assert.equal("deserializeIntoProfile" in models, false);

const writer = new JsonSerializationWriter();
writer.writeObjectValue(undefined, widget, serializeWidget);
const serialized = JSON.parse(new TextDecoder().decode(writer.getSerializedContent()));
assert.equal(serialized.id, payload.id);
assert.equal(serialized.kind, payload.kind);
assert.equal(serialized.marker, payload.marker);
assert.equal(serialized.name, payload.name);
assert.equal(serialized.friends[0].name, payload.friends[0].name);
assert.equal(serialized.extra, payload.extra);
assert.equal(serialized.choice.name, payload.choice.name);
assert.equal("id" in serialized.choice, false);
assert.equal("baseOptional" in serialized, false);
assert.equal("drop" in serialized, false);
assert.equal("profile" in serialized, false);

assert.equal("id" in deserializeIntoRequiredChoice({}), true);
assert.equal("id" in deserializeIntoOptionalChoice({}), false);
const requiredWriter = new JsonSerializationWriter();
requiredWriter.writeObjectValue(undefined, { id: "required", keep: "kept" }, serializeRequiredChoice);
assert.equal(JSON.parse(new TextDecoder().decode(requiredWriter.getSerializedContent())).id, "required");
const optionalWriter = new JsonSerializationWriter();
optionalWriter.writeObjectValue(undefined, { name: "optional" }, serializeOptionalChoice);
const optional = JSON.parse(new TextDecoder().decode(optionalWriter.getSerializedContent()));
assert.equal(optional.name, "optional");
assert.equal("id" in optional, false);
console.log("TypeScript property-filter serialization, inheritance, recursion, and union checks passed.");
