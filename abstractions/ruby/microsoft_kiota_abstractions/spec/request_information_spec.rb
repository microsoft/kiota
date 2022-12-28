require 'microsoft_kiota_abstractions'
require_relative 'request_option_mock'

RSpec.describe MicrosoftKiotaAbstractions do

    it "adds a request option to the request information" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        mock_option = RequestOptionMock.new
        mock_option.value = "value"
        request_info.add_request_options(mock_option)
        expect(request_info.get_request_options().length).to eq(1)
        expect(request_info.get_request_option("key")).to eq(mock_option)
    end

    it "removes a request option from the request information" do
        request_info = MicrosoftKiotaAbstractions::RequestInformation.new
        mock_option = RequestOptionMock.new
        mock_option.value = "value"
        request_info.add_request_options(mock_option)
        expect(request_info.get_request_options().length).to eq(1)
        request_info.remove_request_options("key")
        expect(request_info.get_request_options().length).to eq(0)
    end
end