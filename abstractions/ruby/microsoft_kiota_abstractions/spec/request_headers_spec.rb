require 'microsoft_kiota_abstractions'
RSpec.describe MicrosoftKiotaAbstractions do
    it "adds a request header to the request information" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        request_info.headers.add("key", "value")
        expect(request_info.headers.get_all.length).to eq(1)
        expect(request_info.headers.get("key").length).to eq(1)
        expect(request_info.headers.get("key").first).to eq("value")
    end

    it "adds a request header to the request information when one value is already present" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        request_info.headers.add("key", "value")
        request_info.headers.add("key", "value2")
        expect(request_info.headers.get_all.length).to eq(1)
        expect(request_info.headers.get("key").length).to eq(2)
        expect(request_info.headers.get("key").first).to eq("value")
        expect(request_info.headers.get("key").last).to eq("value2")
    end

    it "removes a request header from the request information" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        request_info.headers.add("key", "value")
        expect(request_info.headers.get_all.length).to eq(1)
        request_info.headers.remove("key")
        expect(request_info.headers.get_all.length).to eq(0)
    end

    it "doesnt fail when removing a value if none are present" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        request_info.headers.remove("key")
        expect(request_info.headers.get_all.length).to eq(0)
    end

    it "clears the request headers from the request information" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        request_info.headers.add("key", "value")
        expect(request_info.headers.get_all.length).to eq(1)
        request_info.headers.clear
        expect(request_info.headers.get_all.length).to eq(0)
    end
end