# frozen_string_literal: true

RSpec.describe Integration_test do
  let(:models) { Integration_test::Client::Models }

  it "reads enum members by their wire values" do
    request_adapter = MicrosoftKiotaFaraday::FaradayRequestAdapter.new(MicrosoftKiotaAbstractions::AnonymousAuthenticationProvider.new)
    request_adapter.set_base_url("http://127.0.0.1:1080")
    game = Integration_test::Client::ApiClient.new(request_adapter).games.latest.get.resume

    expect(game.opening).to eq(models::MoveName[:RockCrushes])
    expect(game.moves).to eq([models::MoveName[:PaperWraps], models::MoveName[:ScissorsCut]])
    expect(game.result).to eq(models::GameResult[:Draw])
  end

  it "writes enum members as their wire values" do
    game = models::Game.new
    game.opening = models::MoveName[:RockCrushes]
    game.moves = [models::MoveName[:PaperWraps], models::MoveName[:ScissorsCut]]
    game.result = models::GameResult[:Draw]
    writer = MicrosoftKiotaSerializationJson::JsonSerializationWriter.new
    writer.write_object_value(nil, game)

    expect(JSON.parse(writer.get_serialized_content)).to eq("opening" => "rock-crushes", "moves" => %w[paper-wraps scissors-cut], "result" => "draw")
  end
end
