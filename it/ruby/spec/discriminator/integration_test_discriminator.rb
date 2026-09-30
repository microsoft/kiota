# frozen_string_literal: true

RSpec.describe Integration_test do
  def deserialize(hash)
    MicrosoftKiotaSerializationJson::JsonParseNodeFactory.new.get_parse_node("application/json", hash.to_json)
                                                         .get_object_value(Integration_test::Client::Discriminateme::Component.method(:create_from_discriminator_value))
  end

  def serialize(value)
    writer = MicrosoftKiotaSerializationJson::JsonSerializationWriterFactory.new.get_serialization_writer("application/json")
    writer.write_object_value(nil, value)
    JSON.parse(writer.get_serialized_content)
  end

  it "deserializes the first member of the union" do
    result = deserialize("objectType" => "obj1", "one" => "foo")

    expect(result.component2).to be_nil
    expect(result.component1).to be_a(Integration_test::Client::Models::Component1)
    expect([result.component1.object_type, result.component1.one]).to eq(%w[obj1 foo])
  end

  it "deserializes the second member of the union" do
    result = deserialize("objectType" => "obj2", "two" => "bar")

    expect(result.component1).to be_nil
    expect(result.component2).to be_a(Integration_test::Client::Models::Component2)
    expect([result.component2.object_type, result.component2.two]).to eq(%w[obj2 bar])
  end

  it "serializes the first member of the union" do
    component = Integration_test::Client::Discriminateme::Component.new
    component.component1 = Integration_test::Client::Models::Component1.new
    component.component1.object_type = "obj1"
    component.component1.one = "foo"

    expect(serialize(component)).to eq("objectType" => "obj1", "one" => "foo")
  end

  it "serializes the second member of the union" do
    component = Integration_test::Client::Discriminateme::Component.new
    component.component2 = Integration_test::Client::Models::Component2.new
    component.component2.object_type = "obj2"
    component.component2.two = "bar"

    expect(serialize(component)).to eq("objectType" => "obj2", "two" => "bar")
  end

  describe "a union without a discriminator" do
    let(:grant) { Integration_test::Client::Grants::GrantsPostResponse }

    def read_grant(hash)
      MicrosoftKiotaSerializationJson::JsonParseNodeFactory.new.get_parse_node("application/json", hash.to_json)
                                                           .get_object_value(grant.method(:create_from_discriminator_value))
    end

    it "reads the payload into every object member, fields they share included" do
      result = read_grant("interact" => "https://auth/interact", "continue" => "https://auth/continue")

      expect(result.pending_grant.interact).to eq("https://auth/interact")
      expect(result.pending_grant.continue).to eq("https://auth/continue")
      expect(result.approved_grant.continue).to eq("https://auth/continue")
      expect(result.approved_grant.access_token).to be_nil
    end

    it "writes the member that is set" do
      result = grant.new
      result.approved_grant = Integration_test::Client::Models::ApprovedGrant.new
      result.approved_grant.access_token = "token"
      result.approved_grant.continue = "https://auth/continue"

      expect(serialize(result)).to eq("accessToken" => "token", "continue" => "https://auth/continue")
    end
  end
end
