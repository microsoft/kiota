# frozen_string_literal: true

require_relative 'spec_helper'
require_relative './files/files.rb'
require_relative '../lib/microsoft_kiota_serialization'
require 'json'

RSpec.describe MicrosoftKiotaSerialization do
  it "has a version number" do
    expect(MicrosoftKiotaSerialization::VERSION).not_to be nil
  end

  it "can build jsonParseNode" do
    json_parse_node = MicrosoftKiotaSerialization::JsonParseNode.new(JSON.parse('{"value": [{"hasAttachments": false}] }'))
    expect(json_parse_node).not_to be nil
    expect(json_parse_node.get_string_value()).to eq("{\"value\"=>[{\"hasAttachments\"=>false}]}")
  end

  it "can deserialize payload" do
    file = File.open("#{File.dirname(__FILE__)}/sample.json")
    data = file.read
    file.close
    message_response = MicrosoftKiotaSerialization::JsonParseNodeFactory.new().get_parse_node("application/json", data)
    object_value = message_response.get_object_value(Files::MessagesResponse)
    
    ## Object Value tests
    expect(object_value.instance_of? Files::MessagesResponse).to eq(true)
    expect(object_value.value[0].instance_of? Files::Message).to eq(true)
    expect(object_value.value[0].body.instance_of? Files::ItemBody).to eq(true)
    expect(object_value.value[0].cc_recipients[0].instance_of? Files::Recipient).to eq(true)
    expect(object_value.value[0].cc_recipients[0].email_address.instance_of? Files::EmailAddress).to eq(true)

    ## String tests
    expect(object_value.next_link.instance_of? String).to eq(true)
    expect(object_value.value[0].body_preview.instance_of? String).to eq(true)
    
    ## Boolean tests
    expect(object_value.value[0].is_draft.to_s.downcase == "true" || object_value.value[0].is_draft.to_s.downcase == "false").to eq(true)
    expect(object_value.value[0].is_read.to_s.downcase == "true" || object_value.value[0].is_read.to_s.downcase == "false").to eq(true)
    expect(object_value.value[0].body_preview.to_s.downcase == "true" || object_value.value[0].body_preview.to_s.downcase == "false").to eq(false)
    
    ## Number tests
    expect(object_value.additional_data["numb"].instance_of? Integer).to eq(true)

    ## GUID tests
    expect(object_value.value[0].guid_id.instance_of? UUIDTools::UUID).to eq(true)

    ## Date tests
    expect(object_value.value[0].received_date_time.instance_of? Time).to eq(true)
    expect(object_value.value[0].sent_date_time.instance_of? Time).to eq(true)
    
    ## Collection of Primitive values tests
    expect(object_value.additional_data["primativeValues"].instance_of? Array).to eq(true)

    ## Collection of object values tests
    expect(object_value.value.instance_of? Array).to eq(true)

    ## Enum tests
    expect(object_value.value[0].body.content_type.instance_of? Symbol).to eq(true)
  end

  # TODO: finish up the test for the serializer as well
  it "can serialize payload" do
    file = File.open("#{File.dirname(__FILE__)}/sample.json")
    data = file.read #JSON.parse(file.read)
    file.close
    message_response = MicrosoftKiotaSerialization::JsonParseNodeFactory.new().get_parse_node("application/json", data)
    object_value = message_response.get_object_value(Files::MessagesResponse)
    serializer = MicrosoftKiotaSerialization::JsonSerializationWriterFactory.new().get_serialization_writer("application/json")
    # serializer.write_object_value(nil, object_value)
    expect(serializer).not_to be nil
  end
end
