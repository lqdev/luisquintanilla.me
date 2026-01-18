const fs = require('fs').promises;
const path = require('path');

/**
 * Followers management utility
 * Uses simple file-based storage following "simpler is better" principle
 */

const FOLLOWERS_FILE = path.join(__dirname, '../data/followers.json');

/**
 * Get current followers collection
 * @returns {Promise<Object>} Followers collection
 */
async function getFollowers() {
  try {
    const data = await fs.readFile(FOLLOWERS_FILE, 'utf8');
    return JSON.parse(data);
  } catch (error) {
    // Return empty collection if file doesn't exist
    return {
      "@context": "https://www.w3.org/ns/activitystreams",
      "id": "https://lqdev.me/api/followers",
      "type": "OrderedCollection",
      "totalItems": 0,
      "orderedItems": []
    };
  }
}

/**
 * Add a follower to the collection
 * @param {string} followerActorId - Actor ID of the follower
 * @returns {Promise<void>}
 */
async function addFollower(followerActorId) {
  const followers = await getFollowers();
  
  // Check if already following
  if (!followers.orderedItems.includes(followerActorId)) {
    followers.orderedItems.push(followerActorId);
    followers.totalItems = followers.orderedItems.length;
    
    await fs.writeFile(FOLLOWERS_FILE, JSON.stringify(followers, null, 2));
  }
}

/**
 * Remove a follower from the collection
 * @param {string} followerActorId - Actor ID of the follower
 * @returns {Promise<void>}
 */
async function removeFollower(followerActorId) {
  const followers = await getFollowers();
  
  const index = followers.orderedItems.indexOf(followerActorId);
  if (index !== -1) {
    followers.orderedItems.splice(index, 1);
    followers.totalItems = followers.orderedItems.length;
    
    await fs.writeFile(FOLLOWERS_FILE, JSON.stringify(followers, null, 2));
  }
}

/**
 * Check if an actor is following
 * @param {string} followerActorId - Actor ID to check
 * @returns {Promise<boolean>}
 */
async function isFollower(followerActorId) {
  const followers = await getFollowers();
  return followers.orderedItems.includes(followerActorId);
}

/**
 * Get all follower actor IDs
 * @returns {Promise<string[]>}
 */
async function getAllFollowers() {
  const followers = await getFollowers();
  return followers.orderedItems;
}

module.exports = {
  getFollowers,
  addFollower,
  removeFollower,
  isFollower,
  getAllFollowers
};
