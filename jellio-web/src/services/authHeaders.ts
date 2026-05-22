const CLIENT_VERSION = '1.5.0';

export const buildAuthHeaders = ({
  token,
  deviceId,
}: {
  token: string;
  deviceId: string;
}) => ({
  Authorization: `MediaBrowser Token="${token}"`,
  'X-Emby-Token': token,
  'X-Emby-Authorization': `MediaBrowser Client="Jellio++", Device="Web", DeviceId="${deviceId}", Version="${CLIENT_VERSION}", Token="${token}"`,
});
