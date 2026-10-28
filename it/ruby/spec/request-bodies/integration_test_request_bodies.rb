# frozen_string_literal: true

RSpec.describe Integration_test do
  let(:client) do
    adapter = MicrosoftKiotaFaraday::FaradayRequestAdapter.new(MicrosoftKiotaAbstractions::AnonymousAuthenticationProvider.new)
    adapter.set_base_url("http://127.0.0.1:1080")
    Integration_test::Client::ApiClient.new(adapter)
  end
  let(:signup) do
    Integration_test::Client::Models::Signup.new.tap do |signup|
      signup.name = "Ann Lee"
      signup.age = 30
      signup.active = false
      signup.tags = %w[a b]
    end
  end

  it "sends a form body" do
    request_info = client.forms.to_post_request_information(signup)
    expect(request_info.headers.get("Content-Type")).to eq(["application/x-www-form-urlencoded"])
    expect(request_info.content).to eq("active=false&age=30&name=Ann+Lee&tags=a&tags=b")
  end

  it "sends a multipart body with a model part and a file part" do
    body = MicrosoftKiotaAbstractions::MultipartBody.new
    body.add_or_replace_part("metadata", "application/json", signup)
    body.add_or_replace_part("file", "application/octet-stream", "\xFF\x00binary".b, "a.bin")
    request_info = client.uploads.to_post_request_information(body)
    boundary = body.boundary
    expect(request_info.headers.get("Content-Type")).to eq(["multipart/form-data; boundary=#{boundary}"])
    expect(request_info.content).to eq(
      "--#{boundary}\r\nContent-Type: application/json\r\nContent-Disposition: form-data; name=\"metadata\"\r\n\r\n" \
      "{\"active\":false,\"age\":30,\"name\":\"Ann Lee\",\"tags\":[\"a\",\"b\"]}\r\n" \
      "--#{boundary}\r\nContent-Type: application/octet-stream\r\nContent-Disposition: form-data; name=\"file\"; filename=\"a.bin\"\r\n\r\n" \
      "\xFF\x00binary\r\n--#{boundary}--\r\n".b
    )
  end

  it "sends a text body" do
    request_info = client.notes.to_post_request_information("hi there")
    expect(request_info.headers.get("Content-Type")).to eq(["text/plain"])
    expect(request_info.content).to eq("hi there")
  end

  it "registers the text and form parse nodes for responses" do
    client
    factories = MicrosoftKiotaAbstractions::ParseNodeFactoryRegistry.default_instance.content_type_associated_factories
    expect(factories["text/plain"]).to be_a(MicrosoftKiotaSerializationText::TextParseNodeFactory)
    expect(factories["application/x-www-form-urlencoded"]).to be_a(MicrosoftKiotaSerializationForm::FormParseNodeFactory)
  end
end
