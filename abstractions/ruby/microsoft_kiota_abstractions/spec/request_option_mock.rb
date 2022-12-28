require 'microsoft_kiota_abstractions'
class RequestOptionMock
  include MicrosoftKiotaAbstractions::RequestOption
  attr_accessor :value
  def get_key()
    return "key"
  end
end