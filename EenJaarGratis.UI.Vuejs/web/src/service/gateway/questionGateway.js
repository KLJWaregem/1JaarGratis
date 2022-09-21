import axios from 'axios';

const api_root = process.env.VUE_APP_TITLE;
export default {
    async get(){

        try{
            let response = await axios.get(api_root + "/Question");
            return response.data;
        }
        catch (err){
            console.error(err.response.data);
            return [];
        }
    },
    async delete(player){
        try{
            await axios.delete(api_root + "/Question/"+{...player}.id);
        }
        catch (err){
            console.error(err.response.data);
            return [];
        }
    },
    async post(question){
        try{
            await axios.post(api_root + "/Question", question);
        }
        catch (err){
            console.error(err.response.data);
            return [];
        }
    },
    async put(question){
        try{
            await axios.put(api_root + "/Question", question);
        }
        catch (err){
            console.error(err.response.data);
            return [];
        }
    },

    async getQuestionGroups(question){
        try{
            let response = await axios.get(api_root + "/Question/" + question.id + "/QuestionGroup");
            return response.data;
        }
        catch (err){
            console.error(err.response.data);
            return [];
        }
    },

    async createQuestionGroups(question){
        try{
            let response = await axios.post(api_root + "/Question/" + question.id + "/QuestionGroup", {});
            return response.data;
        }
        catch (err){
            console.error(err.response.data);
            return [];
        }
    },
    async deleteQuestionGroups(question, questionGroup){
        try{
            let response = await axios.delete(api_root + "/Question/" + question.id + "/QuestionGroup/" + questionGroup.id);
            return response.data;
        }
        catch (err){
            console.error(err);
            return {}
        }
    },

}