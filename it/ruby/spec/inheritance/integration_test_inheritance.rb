# frozen_string_literal: true

RSpec.describe Integration_test do
  let(:client) do
    request_adapter = MicrosoftKiotaFaraday::FaradayRequestAdapter.new(MicrosoftKiotaAbstractions::AnonymousAuthenticationProvider.new)
    request_adapter.set_base_url("http://127.0.0.1:1080")
    Integration_test::Client::ApiClient.new(request_adapter)
  end

  it "deserializes a response typed as the base class into the most derived class" do
    animal = client.api.v1.animals.get.resume

    expect(animal).to be_a(Integration_test::Client::Models::Kitten)
    expect([animal.kind, animal.name, animal.lives, animal.age]).to eq(["kitten", "Tom", 9, 1])
  end

  it "serializes the properties of every level" do
    kitten = Integration_test::Client::Models::Kitten.new
    kitten.kind = "kitten"
    kitten.name = "Tom"
    kitten.lives = 9
    kitten.age = 1
    writer = MicrosoftKiotaSerializationJson::JsonSerializationWriter.new
    writer.write_object_value(nil, kitten)

    expect(JSON.parse(writer.get_serialized_content)).to eq("kind" => "kitten", "name" => "Tom", "lives" => 9, "age" => 1)
  end
end
