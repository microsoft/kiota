<?php
namespace Microsoft\Kiota\Abstractions\Authentication;

use Http\Promise\Promise;
use Microsoft\Kiota\Abstractions\RequestInformation;

/**
 * Interface AuthenticationProvider
 *
 * Authenticates the application request
 *
 * @package Microsoft\Kiota\Abstractions\Authentication
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 * @link https://developer.microsoft.com/graph
 */
interface AuthenticationProvider {
    /**
     * @param RequestInformation $request
     * @return Promise
     */
    public function authenticateRequest(RequestInformation $request): Promise;
}
