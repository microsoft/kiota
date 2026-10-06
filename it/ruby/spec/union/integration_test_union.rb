# frozen_string_literal: true

RSpec.describe Integration_test do
  let(:grant) { Integration_test::Client::Grants::GrantsPostResponse }

  def read_grant(hash)
    MicrosoftKiotaSerializationJson::JsonParseNodeFactory.new.get_parse_node("application/json", hash.to_json)
                                                         .get_object_value(grant.method(:create_from_discriminator_value))
  end

  def serialize(value)
    writer = MicrosoftKiotaSerializationJson::JsonSerializationWriterFactory.new.get_serialization_writer("application/json")
    writer.write_object_value(nil, value)
    JSON.parse(writer.get_serialized_content)
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

  it "writes every member that is set" do
    result = grant.new
    result.pending_grant = Integration_test::Client::Models::PendingGrant.new
    result.pending_grant.interact = "https://auth/interact"
    result.approved_grant = Integration_test::Client::Models::ApprovedGrant.new
    result.approved_grant.access_token = "token"
    result.approved_grant.continue = "https://auth/continue"

    expect(serialize(result)).to eq("interact" => "https://auth/interact", "accessToken" => "token",
                                    "continue" => "https://auth/continue")
  end
end
